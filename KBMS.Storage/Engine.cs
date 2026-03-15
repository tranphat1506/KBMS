using System.IO;
using KBMS.Models;

namespace KBMS.Storage;

public class StorageEngine
{
    private readonly string _dataDir;
    private readonly Encryption _encryption;
    private readonly IndexManager _indexManager;
    private readonly WalManager _wal;

    public StorageEngine(string dataDir, string encryptionKey)
    {
        _dataDir = dataDir;
        _encryption = new Encryption(encryptionKey);
        _indexManager = new IndexManager();
        _wal = new WalManager();

        Directory.CreateDirectory(_dataDir);
    }

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
        var metadataPath = Path.Combine(kbPath, "metadata.bin");

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
        return true;
    }

    public List<KnowledgeBase> ListKbs()
    {
        var kbs = new List<KnowledgeBase>();
        if (!Directory.Exists(_dataDir))
            return kbs;

        foreach (var dir in Directory.GetDirectories(_dataDir))
        {
            var metadataPath = Path.Combine(dir, "metadata.bin");
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
        var metadataPath = Path.Combine(kbPath, "metadata.bin");

        var data = BinaryFormat.Serialize(kb, _encryption);
        File.WriteAllBytes(metadataPath, data);
    }

    // ==================== Object CRUD ====================

    public bool InsertObject(string kbName, ObjectInstance obj)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var objectsPath = Path.Combine(kbPath, "objects.bin");

        _wal.WriteLog(kbPath, $"INSERT_OBJECT:{obj.Id}");

        var objects = LoadAllObjects(kbName);
        objects.Add(obj);

        var data = BinaryFormat.Serialize(objects, _encryption);
        File.WriteAllBytes(objectsPath, data);

        _indexManager.AddIndex(kbPath, obj.Id, obj.ConceptName, obj.Values);

        var kb = LoadKb(kbName);
        if (kb != null)
        {
            kb.ObjectCount = objects.Count;
            SaveKbMetadata(kb);
        }

        _wal.Commit(kbPath);
        return true;
    }

    public List<ObjectInstance> SelectObjects(string kbName, Dictionary<string, object>? conditions = null)
    {
        var objects = LoadAllObjects(kbName);

        if (conditions == null || conditions.Count == 0)
            return objects;

        return objects.Where(obj =>
            conditions.All(kv =>
                obj.Values.Keys.Any(k => k.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)) &&
                obj.Values[kv.Key]?.ToString()?.Equals(kv.Value?.ToString(), StringComparison.OrdinalIgnoreCase) == true
            )
        ).ToList();
    }

    public bool UpdateObject(string kbName, Guid objId, Dictionary<string, object> values)
    {
        var kbPath = Path.Combine(_dataDir, kbName);

        _wal.WriteLog(kbPath, $"UPDATE_OBJECT:{objId}");

        var objects = LoadAllObjects(kbName);
        var obj = objects.FirstOrDefault(o => o.Id == objId);

        if (obj == null)
            return false;

        foreach (var kv in values)
        {
            obj.Values[kv.Key] = kv.Value;
        }

        var objectsPath = Path.Combine(kbPath, "objects.bin");
        var data = BinaryFormat.Serialize(objects, _encryption);
        File.WriteAllBytes(objectsPath, data);

        _indexManager.UpdateIndex(kbPath, objId, obj.ConceptName, obj.Values);
        _wal.Commit(kbPath);
        return true;
    }

    public bool DeleteObject(string kbName, Guid objId)
    {
        var kbPath = Path.Combine(_dataDir, kbName);

        _wal.WriteLog(kbPath, $"DELETE_OBJECT:{objId}");

        var objects = LoadAllObjects(kbName);
        var obj = objects.FirstOrDefault(o => o.Id == objId);

        if (obj == null)
            return false;

        objects.Remove(obj);

        var objectsPath = Path.Combine(kbPath, "objects.bin");
        var data = BinaryFormat.Serialize(objects, _encryption);
        File.WriteAllBytes(objectsPath, data);

        _indexManager.RemoveIndex(kbPath, objId);
        _wal.Commit(kbPath);

        var kb = LoadKb(kbName);
        if (kb != null)
        {
            kb.ObjectCount = objects.Count;
            SaveKbMetadata(kb);
        }

        return true;
    }

    private List<ObjectInstance> LoadAllObjects(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var objectsPath = Path.Combine(kbPath, "objects.bin");

        if (!File.Exists(objectsPath))
            return new List<ObjectInstance>();

        var data = File.ReadAllBytes(objectsPath);
        return BinaryFormat.Deserialize<List<ObjectInstance>>(data, _encryption);
    }

    // ==================== Concept CRUD ====================

    public Concept? CreateConcept(string kbName, Concept concept)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        concept.Id = Guid.NewGuid();
        concept.KbId = kb.Id;

        var concepts = LoadAllConcepts(kbName);
        if (concepts.Any(c => c.Name == concept.Name))
            return null; // Concept with this name already exists

        concepts.Add(concept);
        SaveConcepts(kbName, concepts);

        return concept;
    }

    public Concept? LoadConcept(string kbName, string conceptName)
    {
        var concepts = LoadAllConcepts(kbName);
        return concepts.FirstOrDefault(c => c.Name == conceptName);
    }

    public List<Concept> ListConcepts(string kbName)
    {
        return LoadAllConcepts(kbName);
    }

    public bool DropConcept(string kbName, string conceptName)
    {
        var concepts = LoadAllConcepts(kbName);
        var concept = concepts.FirstOrDefault(c => c.Name == conceptName);

        if (concept == null)
            return false;

        // Check if concept is being used by any objects
        var objects = LoadAllObjects(kbName);
        if (objects.Any(o => o.ConceptName == conceptName))
            return false; // Concept is in use

        concepts.Remove(concept);
        SaveConcepts(kbName, concepts);

        return true;
    }

    private List<Concept> LoadAllConcepts(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var conceptsPath = Path.Combine(kbPath, "concepts.bin");

        if (!File.Exists(conceptsPath))
            return new List<Concept>();

        var data = File.ReadAllBytes(conceptsPath);
        return BinaryFormat.Deserialize<List<Concept>>(data, _encryption);
    }

    private void SaveConcepts(string kbName, List<Concept> concepts)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var conceptsPath = Path.Combine(kbPath, "concepts.bin");

        var data = BinaryFormat.Serialize(concepts, _encryption);
        File.WriteAllBytes(conceptsPath, data);
    }

    public bool AddVariableToConcept(string kbName, string conceptName, string varName, string varType, int? length, int? scale)
    {
        var concepts = LoadAllConcepts(kbName);
        var concept = concepts.FirstOrDefault(c => c.Name == conceptName);

        if (concept == null)
            return false;

        if (concept.Variables.Any(v => v.Name == varName))
            return false; // Variable already exists

        concept.Variables.Add(new Variable { Name = varName, Type = varType, Length = length, Scale = scale });
        SaveConcepts(kbName, concepts);

        return true;
    }

    // ==================== Relation CRUD ====================

    public Relation? CreateRelation(string kbName, Relation relation)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        relation.Id = Guid.NewGuid();
        relation.KbId = kb.Id;

        var relations = LoadAllRelations(kbName);
        if (relations.Any(r => r.Name == relation.Name))
            return null; // Relation with this name already exists

        relations.Add(relation);
        SaveRelations(kbName, relations);

        return relation;
    }

    public List<Relation> ListRelations(string kbName)
    {
        return LoadAllRelations(kbName);
    }

    public bool DropRelation(string kbName, string relationName)
    {
        var relations = LoadAllRelations(kbName);
        var relation = relations.FirstOrDefault(r => r.Name == relationName);

        if (relation == null)
            return false;

        relations.Remove(relation);
        SaveRelations(kbName, relations);

        return true;
    }

    private List<Relation> LoadAllRelations(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var relationsPath = Path.Combine(kbPath, "relations.bin");

        if (!File.Exists(relationsPath))
            return new List<Relation>();

        var data = File.ReadAllBytes(relationsPath);
        return BinaryFormat.Deserialize<List<Relation>>(data, _encryption);
    }

    private void SaveRelations(string kbName, List<Relation> relations)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var relationsPath = Path.Combine(kbPath, "relations.bin");

        var data = BinaryFormat.Serialize(relations, _encryption);
        File.WriteAllBytes(relationsPath, data);
    }

    // ==================== Operator CRUD ====================

    public Operator? CreateOperator(string kbName, Operator op)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        op.Id = Guid.NewGuid();
        op.KbId = kb.Id;

        var operators = LoadAllOperators(kbName);
        if (operators.Any(o => o.Symbol == op.Symbol))
            return null; // Operator with this symbol already exists

        operators.Add(op);
        SaveOperators(kbName, operators);

        return op;
    }

    public List<Operator> ListOperators(string kbName)
    {
        return LoadAllOperators(kbName);
    }

    public bool DropOperator(string kbName, string symbol)
    {
        var operators = LoadAllOperators(kbName);
        var op = operators.FirstOrDefault(o => o.Symbol == symbol);

        if (op == null)
            return false;

        operators.Remove(op);
        SaveOperators(kbName, operators);

        return true;
    }

    private List<Operator> LoadAllOperators(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var operatorsPath = Path.Combine(kbPath, "operators.bin");

        if (!File.Exists(operatorsPath))
            return new List<Operator>();

        var data = File.ReadAllBytes(operatorsPath);
        return BinaryFormat.Deserialize<List<Operator>>(data, _encryption);
    }

    private void SaveOperators(string kbName, List<Operator> operators)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var operatorsPath = Path.Combine(kbPath, "operators.bin");

        var data = BinaryFormat.Serialize(operators, _encryption);
        File.WriteAllBytes(operatorsPath, data);
    }

    // ==================== Function CRUD ====================

    public Function? CreateFunction(string kbName, Function func)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        func.Id = Guid.NewGuid();
        func.KbId = kb.Id;

        var functions = LoadAllFunctions(kbName);
        if (functions.Any(f => f.Name == func.Name))
            return null; // Function with this name already exists

        functions.Add(func);
        SaveFunctions(kbName, functions);

        return func;
    }

    public List<Function> ListFunctions(string kbName)
    {
        return LoadAllFunctions(kbName);
    }

    public bool DropFunction(string kbName, string functionName)
    {
        var functions = LoadAllFunctions(kbName);
        var func = functions.FirstOrDefault(f => f.Name == functionName);

        if (func == null)
            return false;

        functions.Remove(func);
        SaveFunctions(kbName, functions);

        return true;
    }

    private List<Function> LoadAllFunctions(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var functionsPath = Path.Combine(kbPath, "functions.bin");

        if (!File.Exists(functionsPath))
            return new List<Function>();

        var data = File.ReadAllBytes(functionsPath);
        return BinaryFormat.Deserialize<List<Function>>(data, _encryption);
    }

    private void SaveFunctions(string kbName, List<Function> functions)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var functionsPath = Path.Combine(kbPath, "functions.bin");

        var data = BinaryFormat.Serialize(functions, _encryption);
        File.WriteAllBytes(functionsPath, data);
    }

    // ==================== Rule CRUD ====================

    public Rule? CreateRule(string kbName, Rule rule)
    {
        var kb = LoadKb(kbName);
        if (kb == null)
            return null;

        rule.Id = Guid.NewGuid();
        rule.KbId = kb.Id;

        var rules = LoadAllRules(kbName);
        if (rules.Any(r => r.Name == rule.Name))
            return null; // Rule with this name already exists

        rules.Add(rule);
        SaveRules(kbName, rules);

        // Update KB rule count
        kb.RuleCount = rules.Count;
        SaveKbMetadata(kb);

        return rule;
    }

    public List<Rule> ListRules(string kbName)
    {
        return LoadAllRules(kbName);
    }

    public bool DropRule(string kbName, string ruleName)
    {
        var rules = LoadAllRules(kbName);
        var rule = rules.FirstOrDefault(r => r.Name == ruleName);

        if (rule == null)
            return false;

        rules.Remove(rule);
        SaveRules(kbName, rules);

        // Update KB rule count
        var kb = LoadKb(kbName);
        if (kb != null)
        {
            kb.RuleCount = rules.Count;
            SaveKbMetadata(kb);
        }

        return true;
    }

    private List<Rule> LoadAllRules(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var rulesPath = Path.Combine(kbPath, "rules.bin");

        if (!File.Exists(rulesPath))
            return new List<Rule>();

        var data = File.ReadAllBytes(rulesPath);
        return BinaryFormat.Deserialize<List<Rule>>(data, _encryption);
    }

    private void SaveRules(string kbName, List<Rule> rules)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var rulesPath = Path.Combine(kbPath, "rules.bin");

        var data = BinaryFormat.Serialize(rules, _encryption);
        File.WriteAllBytes(rulesPath, data);
    }

    // ==================== Hierarchy CRUD ====================

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

        var hierarchies = LoadAllHierarchies(kbName);
        if (hierarchies.Any(h =>
            h.ParentConcept == parentConcept &&
            h.ChildConcept == childConcept &&
            h.HierarchyType == hierarchyType))
            return null; // Hierarchy already exists

        hierarchies.Add(hierarchy);
        SaveHierarchies(kbName, hierarchies);

        return hierarchy;
    }

    public List<Hierarchy> ListHierarchies(string kbName)
    {
        return LoadAllHierarchies(kbName);
    }

    public bool RemoveHierarchy(string kbName, string parentConcept, string childConcept, HierarchyType hierarchyType)
    {
        var hierarchies = LoadAllHierarchies(kbName);
        var hierarchy = hierarchies.FirstOrDefault(h =>
            h.ParentConcept == parentConcept &&
            h.ChildConcept == childConcept &&
            h.HierarchyType == hierarchyType);

        if (hierarchy == null)
            return false;

        hierarchies.Remove(hierarchy);
        SaveHierarchies(kbName, hierarchies);

        return true;
    }

    private List<Hierarchy> LoadAllHierarchies(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var hierarchiesPath = Path.Combine(kbPath, "hierarchies.bin");

        if (!File.Exists(hierarchiesPath))
            return new List<Hierarchy>();

        var data = File.ReadAllBytes(hierarchiesPath);
        return BinaryFormat.Deserialize<List<Hierarchy>>(data, _encryption);
    }

    private void SaveHierarchies(string kbName, List<Hierarchy> hierarchies)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var hierarchiesPath = Path.Combine(kbPath, "hierarchies.bin");

        var data = BinaryFormat.Serialize(hierarchies, _encryption);
        File.WriteAllBytes(hierarchiesPath, data);
    }

    // ==================== Computation CRUD ====================

    public bool AddComputation(string kbName, string conceptName, List<string> inputVariables, string resultVariable, string formula, int cost)
    {
        var concepts = LoadAllConcepts(kbName);
        var concept = concepts.FirstOrDefault(c => c.Name == conceptName);

        if (concept == null)
            return false;

        // Check if computation already exists
        if (concept.CompRels.Any(c => c.ResultVariable == resultVariable))
            return false;

        var computation = new ComputationRelation
        {
            Id = Guid.NewGuid(),
            ConceptName = conceptName,
            Flag = 0,
            InputVariables = inputVariables,
            Rank = 0,
            ResultVariable = resultVariable,
            Expression = formula,
            Cost = cost
        };

        concept.CompRels.Add(computation);
        SaveConcepts(kbName, concepts);

        return true;
    }

    public bool RemoveComputation(string kbName, string conceptName, string resultVariable)
    {
        var concepts = LoadAllConcepts(kbName);
        var concept = concepts.FirstOrDefault(c => c.Name == conceptName);

        if (concept == null)
            return false;

        var computation = concept.CompRels.FirstOrDefault(c => c.ResultVariable == resultVariable);
        if (computation == null)
            return false;

        concept.CompRels.Remove(computation);
        SaveConcepts(kbName, concepts);

        return true;
    }

    // ==================== User Management ====================

    private string UsersPath => Path.Combine(_dataDir, "users.bin");

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

        if (!VerifyPassword(password, user.PasswordHash))
            return null;

        return user;
    }

    public List<User> LoadUsers()
    {
        if (!File.Exists(UsersPath))
        {
            // Create default ROOT user if no users exist
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
            SaveUsers(new List<User> { rootUser });
            return new List<User> { rootUser };
        }

        var data = File.ReadAllBytes(UsersPath);
        return BinaryFormat.Deserialize<List<User>>(data, _encryption);
    }

    public void SaveUsers(List<User> users)
    {
        Directory.CreateDirectory(_dataDir);
        var data = BinaryFormat.Serialize(users, _encryption);
        File.WriteAllBytes(UsersPath, data);
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

        if (user == null)
            return false;

        if (!user.KbPrivileges.ContainsKey(kbName))
            return false;

        user.KbPrivileges.Remove(kbName);
        SaveUsers(users);

        return true;
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
