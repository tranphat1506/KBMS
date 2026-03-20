using System.IO;
using KBMS.Models;

namespace KBMS.Storage;

/// <summary>
/// StorageEngine V2 - Two-tier Architecture
///
/// PHYSICAL STORAGE LAYER - Custom branded file extensions:
///   metadata.kmf  - KnowledgeBase metadata
///   concepts.kmf  - Concept schema + rules + constraints
///   rules.kmf     - Standalone rules
///   relations.kmf - Semantic relations
///   operators.kmf - Operators
///   functions.kmf - Functions
///   hierarchies.kmf - Concept hierarchies
///   objects.kdf   - ObjectInstance (Fact) data
///   users.kmf     - User accounts and privileges
///   transactions.klf - Write-Ahead Log (WAL)
///
/// LOGICAL STORAGE LAYER - RAM Buffer Pool:
///   All reads come from an in-memory cache (Singleton Dict per KB).
///   Writes go to RAM first; FlushToDisk() persists to .kdf/.kmf.
///   TCL: BeginTransaction() → shadow staging area → CommitTransaction() flushes, Rollback() discards.
/// </summary>
public class StorageEngine
{
    private readonly string _dataDir;
    private readonly Encryption _encryption;
    private readonly IndexManager _indexManager;
    private readonly WalManager _wal;

    // ==================== BUFFER POOL (RAM Cache) ====================
    // One cache entry per KB name. Loaded lazily on first access.
    private readonly Dictionary<string, List<ObjectInstance>> _objectPool = new();
    private readonly Dictionary<string, List<Concept>> _conceptPool = new();
    private readonly Dictionary<string, List<Rule>> _rulePool = new();
    private readonly Dictionary<string, List<Relation>> _relationPool = new();
    private readonly Dictionary<string, List<Operator>> _operatorPool = new();
    private readonly Dictionary<string, List<Function>> _functionPool = new();
    private readonly Dictionary<string, List<Hierarchy>> _hierarchyPool = new();
    private List<User>? _userPool = null;

    // ==================== TRANSACTION (Shadow Paging) ====================
    // When a transaction is active, all CUD operations go to the shadow instead of main pool.
    private bool _transactionActive = false;
    private Dictionary<string, List<ObjectInstance>>? _shadowObjectPool = null;
    private Dictionary<string, List<Concept>>? _shadowConceptPool = null;

    // ==================== FILE NAME CONSTANTS ====================
    private const string OBJ_EXT = "objects.kdf";
    private const string CONCEPT_EXT = "concepts.kmf";
    private const string RULE_EXT = "rules.kmf";
    private const string RELATION_EXT = "relations.kmf";
    private const string OPERATOR_EXT = "operators.kmf";
    private const string FUNCTION_EXT = "functions.kmf";
    private const string HIERARCHY_EXT = "hierarchies.kmf";
    private const string META_EXT = "metadata.kmf";
    private const string USERS_EXT = "users.kmf";

    // ==================== CONSTRUCTOR ====================

    public StorageEngine(string dataDir, string encryptionKey)
    {
        _dataDir = dataDir;
        _encryption = new Encryption(encryptionKey);
        _indexManager = new IndexManager();
        _wal = new WalManager();

        Directory.CreateDirectory(_dataDir);
    }

    // ==================== TCL: TRANSACTION METHODS ====================

    /// <summary>
    /// Starts a transaction. All subsequent CUD operations go to shadow pools.
    /// No disk I/O until CommitTransaction() is called.
    /// </summary>
    public void BeginTransaction()
    {
        if (_transactionActive) return;
        _transactionActive = true;

        // Deep-copy current pools into shadow
        _shadowObjectPool = _objectPool.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(o => DeepCopyObject(o)).ToList());
        _shadowConceptPool = _conceptPool.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(c => DeepCopyConcept(c)).ToList());
    }

    /// <summary>
    /// Commits all changes: promotes shadow → main pool, then flushes to disk.
    /// WAL is cleared after a successful commit.
    /// </summary>
    public void CommitTransaction(string kbName)
    {
        if (!_transactionActive) return;

        // Promote shadow → main pool
        if (_shadowObjectPool != null)
        {
            foreach (var kv in _shadowObjectPool)
                _objectPool[kv.Key] = kv.Value;
        }
        if (_shadowConceptPool != null)
        {
            foreach (var kv in _shadowConceptPool)
                _conceptPool[kv.Key] = kv.Value;
        }

        // Flush promoted data to physical disk
        if (_objectPool.TryGetValue(kbName, out var objects))
            FlushObjectsToDisk(kbName, objects);
        if (_conceptPool.TryGetValue(kbName, out var concepts))
            FlushConceptsToDisk(kbName, concepts);

        // Seal the WAL log
        var kbPath = Path.Combine(_dataDir, kbName);
        _wal.Commit(kbPath);

        // Clean up shadow
        _shadowObjectPool = null;
        _shadowConceptPool = null;
        _transactionActive = false;
    }

    /// <summary>
    /// Rolls back: discards shadow pools entirely. Disk & main pool untouched.
    /// </summary>
    public void RollbackTransaction()
    {
        _shadowObjectPool = null;
        _shadowConceptPool = null;
        _transactionActive = false;
    }

    /// <summary>
    /// Returns whether a transaction is currently active.
    /// </summary>
    public bool IsTransactionActive() => _transactionActive;

    // ==================== KNOWLEDGE BASE ====================

    public KnowledgeBase CreateKb(string kbName, Guid ownerId, string description = "")
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        Directory.CreateDirectory(kbPath);

        var kb = new KnowledgeBase
        {
            Id = Guid.NewGuid(),
            Name = kbName,
            CreatedAt = DateTime.Now,
            OwnerId = ownerId,
            Description = description,
            ObjectCount = 0,
            RuleCount = 0
        };

        SaveKbMetadata(kb);
        _indexManager.CreateIndex(kbPath);

        return kb;
    }

    public KnowledgeBase? LoadKb(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var metadataPath = Path.Combine(kbPath, META_EXT);

        // Backward-compat: check legacy .bin name as well
        if (!File.Exists(metadataPath))
            metadataPath = Path.Combine(kbPath, "metadata.bin");

        if (!File.Exists(metadataPath))
            return null;

        var data = File.ReadAllBytes(metadataPath);
        return BinaryFormat.Deserialize<KnowledgeBase>(data, _encryption);
    }

    public bool DropKb(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        if (!Directory.Exists(kbPath))
            return false;

        Directory.Delete(kbPath, true);

        // Evict from buffer pools
        _objectPool.Remove(kbName);
        _conceptPool.Remove(kbName);
        _rulePool.Remove(kbName);
        _relationPool.Remove(kbName);
        _operatorPool.Remove(kbName);
        _functionPool.Remove(kbName);
        _hierarchyPool.Remove(kbName);

        return true;
    }

    public List<KnowledgeBase> ListKbs()
    {
        var kbs = new List<KnowledgeBase>();
        if (!Directory.Exists(_dataDir))
            return kbs;

        foreach (var dir in Directory.GetDirectories(_dataDir))
        {
            // Support both new .kmf and legacy .bin
            var metadataPath = Path.Combine(dir, META_EXT);
            if (!File.Exists(metadataPath))
                metadataPath = Path.Combine(dir, "metadata.bin");

            if (File.Exists(metadataPath))
            {
                var data = File.ReadAllBytes(metadataPath);
                try
                {
                    var kb = BinaryFormat.Deserialize<KnowledgeBase>(data, _encryption);
                    kbs.Add(kb);
                }
                catch
                {
                    // Skip corrupted KBs
                }
            }
        }

        return kbs;
    }

    private void SaveKbMetadata(KnowledgeBase kb)
    {
        var kbPath = Path.Combine(_dataDir, kb.Name);
        var metadataPath = Path.Combine(kbPath, META_EXT);
        var data = BinaryFormat.Serialize(kb, _encryption);
        File.WriteAllBytes(metadataPath, data);
    }

    // ==================== OBJECT CRUD (Buffer Pool Aware) ====================

    public bool InsertObject(string kbName, ObjectInstance obj)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        _wal.WriteLog(kbPath, $"INSERT_OBJECT:{obj.Id}");

        var objects = GetObjectPoolOrShadow(kbName);
        objects.Add(obj);

        if (!_transactionActive)
        {
            FlushObjectsToDisk(kbName, objects);
            UpdateKbObjectCount(kbName, objects.Count);
            _wal.Commit(kbPath);
        }

        _indexManager.AddIndex(kbPath, obj.Id, obj.ConceptName, obj.Values);

        return true;
    }

    public List<ObjectInstance> SelectObjects(string kbName, Dictionary<string, object>? conditions = null)
    {
        var objects = GetObjectPoolOrShadow(kbName);

        if (conditions == null || conditions.Count == 0)
            return objects;

        return objects.Where(obj =>
            conditions.All(kv =>
            {
                var key = obj.Values.Keys.FirstOrDefault(k => k.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                return key != null && obj.Values[key]?.ToString()?.Equals(kv.Value?.ToString(), StringComparison.OrdinalIgnoreCase) == true;
            })
        ).ToList();
    }

    public bool UpdateObject(string kbName, Guid objId, Dictionary<string, object> values)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        _wal.WriteLog(kbPath, $"UPDATE_OBJECT:{objId}");

        var objects = GetObjectPoolOrShadow(kbName);
        var obj = objects.FirstOrDefault(o => o.Id == objId);

        if (obj == null)
            return false;

        foreach (var kv in values)
            obj.Values[kv.Key] = kv.Value;

        if (!_transactionActive)
        {
            FlushObjectsToDisk(kbName, objects);
            _wal.Commit(kbPath);
        }

        _indexManager.UpdateIndex(kbPath, objId, obj.ConceptName, obj.Values);
        return true;
    }

    public bool DeleteObject(string kbName, Guid objId)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        _wal.WriteLog(kbPath, $"DELETE_OBJECT:{objId}");

        var objects = GetObjectPoolOrShadow(kbName);
        var obj = objects.FirstOrDefault(o => o.Id == objId);

        if (obj == null)
            return false;

        objects.Remove(obj);

        if (!_transactionActive)
        {
            FlushObjectsToDisk(kbName, objects);
            UpdateKbObjectCount(kbName, objects.Count);
            _wal.Commit(kbPath);
        }

        _indexManager.RemoveIndex(kbPath, objId);
        return true;
    }

    // ==================== CONCEPT CRUD (Buffer Pool Aware) ====================

    public Concept? CreateConcept(string kbName, Concept concept)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        concept.Id = Guid.NewGuid();
        concept.KbId = kb.Id;

        var concepts = GetConceptPoolOrShadow(kbName);
        if (concepts.Any(c => c.Name == concept.Name))
            return null;

        concepts.Add(concept);

        if (!_transactionActive)
            FlushConceptsToDisk(kbName, concepts);

        return concept;
    }

    public Concept? LoadConcept(string kbName, string conceptName)
    {
        var concepts = GetConceptPoolOrShadow(kbName);
        return concepts.FirstOrDefault(c => c.Name == conceptName);
    }

    public List<Concept> ListConcepts(string kbName)
    {
        return GetConceptPoolOrShadow(kbName);
    }

    public bool DropConcept(string kbName, string conceptName)
    {
        var concepts = GetConceptPoolOrShadow(kbName);
        var concept = concepts.FirstOrDefault(c => c.Name == conceptName);

        if (concept == null)
            return false;

        var objects = GetObjectPoolOrShadow(kbName);
        if (objects.Any(o => o.ConceptName == conceptName))
            return false; // Concept is in use

        concepts.Remove(concept);

        if (!_transactionActive)
            FlushConceptsToDisk(kbName, concepts);

        return true;
    }

    public bool AddVariableToConcept(string kbName, string conceptName, string varName, string varType, int? length, int? scale)
    {
        var concepts = GetConceptPoolOrShadow(kbName);
        var concept = concepts.FirstOrDefault(c => c.Name == conceptName);

        if (concept == null)
            return false;

        if (concept.Variables.Any(v => v.Name == varName))
            return false;

        concept.Variables.Add(new Variable { Name = varName, Type = varType, Length = length, Scale = scale });

        if (!_transactionActive)
            FlushConceptsToDisk(kbName, concepts);

        return true;
    }

    // ==================== RELATION CRUD ====================

    public Relation? CreateRelation(string kbName, Relation relation)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        relation.Id = Guid.NewGuid();
        relation.KbId = kb.Id;

        var relations = GetFromPool(_relationPool, kbName, () => LoadFromDisk<List<Relation>>(kbName, RELATION_EXT));
        if (relations.Any(r => r.Name == relation.Name))
            return null;

        relations.Add(relation);
        SaveToDisk(kbName, RELATION_EXT, relations);

        return relation;
    }

    public List<Relation> ListRelations(string kbName)
    {
        return GetFromPool(_relationPool, kbName, () => LoadFromDisk<List<Relation>>(kbName, RELATION_EXT));
    }

    public bool DropRelation(string kbName, string relationName)
    {
        var relations = GetFromPool(_relationPool, kbName, () => LoadFromDisk<List<Relation>>(kbName, RELATION_EXT));
        var relation = relations.FirstOrDefault(r => r.Name == relationName);

        if (relation == null)
            return false;

        relations.Remove(relation);
        SaveToDisk(kbName, RELATION_EXT, relations);
        return true;
    }

    // ==================== OPERATOR CRUD ====================

    public Operator? CreateOperator(string kbName, Operator op)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        op.Id = Guid.NewGuid();
        op.KbId = kb.Id;

        var operators = GetFromPool(_operatorPool, kbName, () => LoadFromDisk<List<Operator>>(kbName, OPERATOR_EXT));
        if (operators.Any(o => o.Symbol == op.Symbol))
            return null;

        operators.Add(op);
        SaveToDisk(kbName, OPERATOR_EXT, operators);
        return op;
    }

    public List<Operator> ListOperators(string kbName)
    {
        return GetFromPool(_operatorPool, kbName, () => LoadFromDisk<List<Operator>>(kbName, OPERATOR_EXT));
    }

    public bool DropOperator(string kbName, string symbol)
    {
        var operators = GetFromPool(_operatorPool, kbName, () => LoadFromDisk<List<Operator>>(kbName, OPERATOR_EXT));
        var op = operators.FirstOrDefault(o => o.Symbol == symbol);

        if (op == null)
            return false;

        operators.Remove(op);
        SaveToDisk(kbName, OPERATOR_EXT, operators);
        return true;
    }

    // ==================== FUNCTION CRUD ====================

    public Function? CreateFunction(string kbName, Function func)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        func.Id = Guid.NewGuid();
        func.KbId = kb.Id;

        var functions = GetFromPool(_functionPool, kbName, () => LoadFromDisk<List<Function>>(kbName, FUNCTION_EXT));
        if (functions.Any(f => f.Name == func.Name))
            return null;

        functions.Add(func);
        SaveToDisk(kbName, FUNCTION_EXT, functions);
        return func;
    }

    public List<Function> ListFunctions(string kbName)
    {
        return GetFromPool(_functionPool, kbName, () => LoadFromDisk<List<Function>>(kbName, FUNCTION_EXT));
    }

    public bool DropFunction(string kbName, string functionName)
    {
        var functions = GetFromPool(_functionPool, kbName, () => LoadFromDisk<List<Function>>(kbName, FUNCTION_EXT));
        var func = functions.FirstOrDefault(f => f.Name == functionName);

        if (func == null)
            return false;

        functions.Remove(func);
        SaveToDisk(kbName, FUNCTION_EXT, functions);
        return true;
    }

    // ==================== RULE CRUD ====================

    public Rule? CreateRule(string kbName, Rule rule)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        rule.Id = Guid.NewGuid();
        rule.KbId = kb.Id;

        var rules = GetFromPool(_rulePool, kbName, () => LoadFromDisk<List<Rule>>(kbName, RULE_EXT));
        if (rules.Any(r => r.Name == rule.Name))
            return null;

        rules.Add(rule);
        SaveToDisk(kbName, RULE_EXT, rules);

        kb.RuleCount = rules.Count;
        SaveKbMetadata(kb);
        return rule;
    }

    public List<Rule> ListRules(string kbName)
    {
        return GetFromPool(_rulePool, kbName, () => LoadFromDisk<List<Rule>>(kbName, RULE_EXT));
    }

    public bool DropRule(string kbName, string ruleName)
    {
        var rules = GetFromPool(_rulePool, kbName, () => LoadFromDisk<List<Rule>>(kbName, RULE_EXT));
        var rule = rules.FirstOrDefault(r => r.Name == ruleName);

        if (rule == null)
            return false;

        rules.Remove(rule);
        SaveToDisk(kbName, RULE_EXT, rules);

        var kb = LoadKb(kbName);
        if (kb != null)
        {
            kb.RuleCount = rules.Count;
            SaveKbMetadata(kb);
        }

        return true;
    }

    // ==================== HIERARCHY CRUD ====================

    public Hierarchy? AddHierarchy(string kbName, string parentConcept, string childConcept, HierarchyType hierarchyType)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        var hierarchy = new Hierarchy
        {
            Id = Guid.NewGuid(),
            KbId = kb.Id,
            ParentConcept = parentConcept,
            ChildConcept = childConcept,
            HierarchyType = hierarchyType
        };

        var hierarchies = GetFromPool(_hierarchyPool, kbName, () => LoadFromDisk<List<Hierarchy>>(kbName, HIERARCHY_EXT));
        if (hierarchies.Any(h =>
            h.ParentConcept == parentConcept &&
            h.ChildConcept == childConcept &&
            h.HierarchyType == hierarchyType))
            return null;

        hierarchies.Add(hierarchy);
        SaveToDisk(kbName, HIERARCHY_EXT, hierarchies);
        return hierarchy;
    }

    public List<Hierarchy> ListHierarchies(string kbName)
    {
        return GetFromPool(_hierarchyPool, kbName, () => LoadFromDisk<List<Hierarchy>>(kbName, HIERARCHY_EXT));
    }

    public bool RemoveHierarchy(string kbName, string parentConcept, string childConcept, HierarchyType hierarchyType)
    {
        var hierarchies = GetFromPool(_hierarchyPool, kbName, () => LoadFromDisk<List<Hierarchy>>(kbName, HIERARCHY_EXT));
        var hierarchy = hierarchies.FirstOrDefault(h =>
            h.ParentConcept == parentConcept &&
            h.ChildConcept == childConcept &&
            h.HierarchyType == hierarchyType);

        if (hierarchy == null)
            return false;

        hierarchies.Remove(hierarchy);
        SaveToDisk(kbName, HIERARCHY_EXT, hierarchies);
        return true;
    }

    // ==================== COMPUTATION CRUD ====================

    public bool AddComputation(string kbName, string conceptName, List<string> inputVariables, string resultVariable, string formula, int cost)
    {
        var concepts = GetConceptPoolOrShadow(kbName);
        var concept = concepts.FirstOrDefault(c => c.Name == conceptName);

        if (concept == null)
            return false;

        if (concept.CompRels.Any(c => c.ResultVariable == resultVariable))
            return false;

        concept.CompRels.Add(new ComputationRelation
        {
            Id = Guid.NewGuid(),
            ConceptName = conceptName,
            Flag = 0,
            InputVariables = inputVariables,
            Rank = 0,
            ResultVariable = resultVariable,
            Expression = formula,
            Cost = cost
        });

        if (!_transactionActive)
            FlushConceptsToDisk(kbName, concepts);

        return true;
    }

    public bool RemoveComputation(string kbName, string conceptName, string resultVariable)
    {
        var concepts = GetConceptPoolOrShadow(kbName);
        var concept = concepts.FirstOrDefault(c => c.Name == conceptName);

        if (concept == null)
            return false;

        var computation = concept.CompRels.FirstOrDefault(c => c.ResultVariable == resultVariable);
        if (computation == null)
            return false;

        concept.CompRels.Remove(computation);

        if (!_transactionActive)
            FlushConceptsToDisk(kbName, concepts);

        return true;
    }

    // ==================== USER MANAGEMENT ====================

    private string UsersPath => Path.Combine(_dataDir, USERS_EXT);
    private string UsersPathLegacy => Path.Combine(_dataDir, "users.bin");

    public User? CreateUser(string username, string password, UserRole role, bool systemAdmin = false)
    {
        var users = LoadUsers();
        if (users.Any(u => u.Username == username))
            return null;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = HashPassword(password),
            Role = role,
            SystemAdmin = systemAdmin,
            KbPrivileges = new Dictionary<string, Privilege>(),
            CreatedAt = DateTime.Now
        };

        users.Add(user);
        SaveUsers(users);
        return user;
    }

    public User? Login(string username, string password)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null)
            return null;

        return !VerifyPassword(password, user.PasswordHash) ? null : user;
    }

    public List<User> LoadUsers()
    {
        // Check RAM cache first
        if (_userPool != null)
            return _userPool;

        var path = File.Exists(UsersPath) ? UsersPath
                 : File.Exists(UsersPathLegacy) ? UsersPathLegacy
                 : null;

        if (path == null)
        {
            // Create default root user
            var rootUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "root",
                PasswordHash = HashPassword("root"),
                Role = UserRole.ROOT,
                SystemAdmin = true,
                KbPrivileges = new Dictionary<string, Privilege>(),
                CreatedAt = DateTime.Now
            };
            _userPool = new List<User> { rootUser };
            SaveUsers(_userPool);
            return _userPool;
        }

        var data = File.ReadAllBytes(path);
        _userPool = BinaryFormat.Deserialize<List<User>>(data, _encryption);
        return _userPool;
    }

    public void SaveUsers(List<User> users)
    {
        Directory.CreateDirectory(_dataDir);
        var data = BinaryFormat.Serialize(users, _encryption);
        File.WriteAllBytes(UsersPath, data);
        _userPool = users;
    }

    public bool DropUser(string username)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null)
            return false;

        users.Remove(user);
        SaveUsers(users);
        return true;
    }

    public bool GrantPrivilege(string kbName, string username, string privilege)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null)
            return false;

        var priv = Enum.TryParse<Privilege>(privilege, out var privEnum) ? privEnum : Privilege.READ;
        user.KbPrivileges[kbName] = priv;
        SaveUsers(users);
        return true;
    }

    public bool RevokePrivilege(string kbName, string username)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null || !user.KbPrivileges.ContainsKey(kbName))
            return false;

        user.KbPrivileges.Remove(kbName);
        SaveUsers(users);
        return true;
    }

    // ==================== BUFFER POOL HELPERS ====================

    /// <summary>Returns the shadow object pool if in a transaction, otherwise the main object pool.</summary>
    private List<ObjectInstance> GetObjectPoolOrShadow(string kbName)
    {
        if (_transactionActive && _shadowObjectPool != null)
        {
            if (!_shadowObjectPool.ContainsKey(kbName))
                _shadowObjectPool[kbName] = LoadObjectsFromDisk(kbName);
            return _shadowObjectPool[kbName];
        }

        if (!_objectPool.ContainsKey(kbName))
            _objectPool[kbName] = LoadObjectsFromDisk(kbName);

        return _objectPool[kbName];
    }

    /// <summary>Returns the shadow concept pool if in a transaction, otherwise the main concept pool.</summary>
    private List<Concept> GetConceptPoolOrShadow(string kbName)
    {
        if (_transactionActive && _shadowConceptPool != null)
        {
            if (!_shadowConceptPool.ContainsKey(kbName))
                _shadowConceptPool[kbName] = LoadConceptsFromDisk(kbName);
            return _shadowConceptPool[kbName];
        }

        if (!_conceptPool.ContainsKey(kbName))
            _conceptPool[kbName] = LoadConceptsFromDisk(kbName);

        return _conceptPool[kbName];
    }

    /// <summary>Generic pool accessor. Used for less-transactional entity types.</summary>
    private List<T> GetFromPool<T>(Dictionary<string, List<T>> pool, string kbName, Func<List<T>> loader)
    {
        if (!pool.ContainsKey(kbName))
            pool[kbName] = loader();
        return pool[kbName];
    }

    // ==================== DISK I/O HELPERS ====================

    private List<ObjectInstance> LoadObjectsFromDisk(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var path = Path.Combine(kbPath, OBJ_EXT);

        // Backward-compat with legacy .bin
        if (!File.Exists(path))
            path = Path.Combine(kbPath, "objects.bin");

        if (!File.Exists(path))
            return new List<ObjectInstance>();

        var data = File.ReadAllBytes(path);
        return BinaryFormat.Deserialize<List<ObjectInstance>>(data, _encryption);
    }

    private List<Concept> LoadConceptsFromDisk(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var path = Path.Combine(kbPath, CONCEPT_EXT);

        if (!File.Exists(path))
            path = Path.Combine(kbPath, "concepts.bin");

        if (!File.Exists(path))
            return new List<Concept>();

        var data = File.ReadAllBytes(path);
        return BinaryFormat.Deserialize<List<Concept>>(data, _encryption);
    }

    private T LoadFromDisk<T>(string kbName, string filename) where T : new()
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var path = Path.Combine(kbPath, filename);

        // Backward-compat: try .bin equivalent
        if (!File.Exists(path))
        {
            var legacyName = Path.GetFileNameWithoutExtension(filename) + ".bin";
            path = Path.Combine(kbPath, legacyName);
        }

        if (!File.Exists(path))
            return new T();

        var data = File.ReadAllBytes(path);
        return BinaryFormat.Deserialize<T>(data, _encryption);
    }

    private void FlushObjectsToDisk(string kbName, List<ObjectInstance> objects)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var data = BinaryFormat.Serialize(objects, _encryption);
        File.WriteAllBytes(Path.Combine(kbPath, OBJ_EXT), data);
    }

    private void FlushConceptsToDisk(string kbName, List<Concept> concepts)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var data = BinaryFormat.Serialize(concepts, _encryption);
        File.WriteAllBytes(Path.Combine(kbPath, CONCEPT_EXT), data);
    }

    private void SaveToDisk<T>(string kbName, string filename, T obj)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var data = BinaryFormat.Serialize(obj, _encryption);
        File.WriteAllBytes(Path.Combine(kbPath, filename), data);
    }

    private void UpdateKbObjectCount(string kbName, int count)
    {
        var kb = LoadKb(kbName);
        if (kb != null)
        {
            kb.ObjectCount = count;
            SaveKbMetadata(kb);
        }
    }

    // ==================== DEEP COPY HELPERS (for Shadow Paging) ====================

    private static ObjectInstance DeepCopyObject(ObjectInstance src)
    {
        return new ObjectInstance
        {
            Id = src.Id,
            ConceptName = src.ConceptName,
            KbId = src.KbId,
            Values = new Dictionary<string, object>(src.Values)
        };
    }

    private static Concept DeepCopyConcept(Concept src)
    {
        return new Concept
        {
            Id = src.Id,
            KbId = src.KbId,
            Name = src.Name,
            Variables = src.Variables.Select(v => new Variable { Name = v.Name, Type = v.Type, Length = v.Length, Scale = v.Scale }).ToList(),
            Constraints = src.Constraints.Select(c => new Constraint { Name = c.Name, Expression = c.Expression, Line = c.Line, Column = c.Column }).ToList(),
            CompRels = src.CompRels.Select(c => new ComputationRelation { Id = c.Id, ConceptName = c.ConceptName, Flag = c.Flag, InputVariables = c.InputVariables.ToList(), Rank = c.Rank, ResultVariable = c.ResultVariable, Expression = c.Expression, Cost = c.Cost }).ToList(),
            Aliases = src.Aliases.ToList(),
            BaseObjects = src.BaseObjects.ToList(),
            SameVariables = src.SameVariables.Select(sv => new SameVariable { Variable1 = sv.Variable1, Variable2 = sv.Variable2 }).ToList(),
            ConstructRelations = src.ConstructRelations.Select(cr => new ConstructRelation { RelationName = cr.RelationName, Arguments = cr.Arguments.ToList() }).ToList(),
            Properties = src.Properties.Select(p => new Property { Key = p.Key, Value = p.Value }).ToList(),
            ConceptRules = src.ConceptRules.Select(r => new ConceptRule { Id = r.Id, Kind = r.Kind, Variables = r.Variables.ToList(), Hypothesis = r.Hypothesis.ToList(), Conclusion = r.Conclusion.ToList() }).ToList(),
            Equations = src.Equations.Select(e => new Equation { Id = e.Id, Expression = e.Expression, Variables = e.Variables.ToList(), Line = e.Line, Column = e.Column }).ToList()
        };
    }

    // ==================== AUTH HELPERS ====================

    private string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
