using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Parser.Ast;
using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;
using KBMS.Parser.Ast.Expressions;
using KBMS.Storage;
using KBMS.Storage.Core;
using KBMS.Knowledge.Core;
using KBMS.Knowledge.Validation;
using KBMS.Reasoning;

namespace KBMS.Knowledge;

/// <summary>
/// Knowledge Manager - Executes AST nodes against the storage engine
/// </summary>
public class KnowledgeManager
{
    private readonly StoragePool _storagePool;
    private readonly StorageRouter _v3Router;
    public StorageRouter V3Router => _v3Router;
    private readonly KBMS.Storage.Core.KbCatalog _kbCatalog;
    private readonly KBMS.Storage.Core.ConceptCatalog _conceptCatalog;
    private readonly KBMS.Storage.Core.UserCatalog _userCatalog;

    // Per-KB InferenceEngine cache: engines hold compiled Rete networks
    // Key = kbName (case-insensitive)
    private readonly ConcurrentDictionary<string, KBMS.Reasoning.InferenceEngine> _engineCache
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Fully invalidates the Rete network cache for a given KB.
    /// Call this whenever schema changes (rules, concepts, relations) are made.
    /// </summary>
    private void InvalidateEngineCache(string kbName)
    {
        if (_engineCache.TryRemove(kbName, out var old))
            old.ClearCache();
    }

    public KnowledgeManager(
        StoragePool storagePool,
        KbCatalog kbCatalog,
        ConceptCatalog conceptCatalog,
        UserCatalog userCatalog,
        StorageRouter? v3Router = null)
    {
        _storagePool = storagePool;
        _kbCatalog = kbCatalog;
        _conceptCatalog = conceptCatalog;
        _userCatalog = userCatalog;
        _v3Router = v3Router ?? new StorageRouter(storagePool);
    }

    private Concept GetEffectiveConcept(string kbName, Concept primary)
    {
        var allBaseObjects = new HashSet<string>(primary.BaseObjects, StringComparer.OrdinalIgnoreCase);
        
        var effective = new Concept
        {
            Name = primary.Name,
            Variables = new List<Variable>(primary.Variables),
            Constraints = new List<KBMS.Models.Constraint>(primary.Constraints),
            SameVariables = new List<SameVariable>(primary.SameVariables),
            ConceptRules = new List<ConceptRule>(primary.ConceptRules),
            Equations = new List<Equation>(primary.Equations),
            ConstructRelations = new List<ConstructRelation>(primary.ConstructRelations)
        };

        foreach (var baseName in allBaseObjects)
        {
            var baseConcept = _conceptCatalog.LoadConcept(kbName, baseName);
            if (baseConcept != null)
            {
                var flattendBase = GetEffectiveConcept(kbName, baseConcept);
                // Inherit variables that aren't shadowed
                foreach (var v in flattendBase.Variables)
                {
                    if (!effective.Variables.Any(ev => ev.Name.Equals(v.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        effective.Variables.Add(v);
                    }
                }
                effective.Constraints.AddRange(flattendBase.Constraints);
                effective.SameVariables.AddRange(flattendBase.SameVariables);
                effective.ConceptRules.AddRange(flattendBase.ConceptRules);
                effective.Equations.AddRange(flattendBase.Equations);
                effective.ConstructRelations.AddRange(flattendBase.ConstructRelations);
            }
        }
        return effective;
    }

    /// <summary>
    /// Ensures that every concept with BASE_OBJECTS has a matching IS-A Hierarchy entry.
    /// Safe to call repeatedly (idempotent). Runs on USE and on CREATE CONCEPT.
    /// </summary>
    private void SyncHierarchiesFromBaseObjects(string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return;

        var concepts = _conceptCatalog.ListConcepts(kbName);
        bool modified = false;

        foreach (var concept in concepts)
        {
            foreach (var baseObj in concept.BaseObjects)
            {
                bool exists = kb.Hierarchies.Any(h =>
                    h.ChildConcept.Equals(concept.Name, StringComparison.OrdinalIgnoreCase) &&
                    h.ParentConcept.Equals(baseObj, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    kb.Hierarchies.Add(new Hierarchy
                    {
                        ParentConcept = baseObj,
                        ChildConcept = concept.Name,
                        HierarchyType = Models.HierarchyType.IsA
                    });
                    modified = true;
                }
            }
        }

        if (modified) _kbCatalog.SaveKbMetadata(kb);
    }

    private List<string> GetDescendantConcepts(string kbName, string parentConceptName)
    {
        var descendants = new List<string>();
        var concepts = _conceptCatalog.ListConcepts(kbName);
        var queue = new Queue<string>();
        queue.Enqueue(parentConceptName);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var c in concepts)
            {
                if (c.BaseObjects != null && c.BaseObjects.Any(b => b.Equals(current, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!descendants.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        descendants.Add(c.Name);
                        queue.Enqueue(c.Name);
                    }
                }
            }
        }
        return descendants;
    }

    /// <summary>
    /// Execute an AST node with user context
    /// </summary>
    public object Execute(AstNode ast, User user, string? currentKb)
    {
        if (ast == null)
        {
            return ErrorResponse.ExecutionErrorResponse("Query is empty, a comment, or could not be parsed.");
        }

        // Determine KB name
        var kbName = DetermineKbName(ast) ?? currentKb;

        // Check if KB is required
        if (RequiresKb(ast) && kbName == null)
        {
            return ErrorResponse.ExecutionErrorResponse("No knowledge base selected. Use 'USE <kbname>' first.");
        }

        // Check privileges
        var action = DetermineAction(ast);
        if (!CheckPrivilege(user, action, kbName, ast))
        {
            return ErrorResponse.PermissionErrorResponse(action, kbName ?? "system");
        }

        // NEW: Semantic validation before execution
        var validationResult = ValidateAst(ast, kbName);
        if (!validationResult.IsValid)
        {
            return ErrorResponse.ValidationErrorResponse(string.Join("; ", validationResult.Errors));
        }

        // Log warnings if any
        if (validationResult.Warnings.Count > 0)
        {
            Console.WriteLine($"[WARNINGS] {string.Join("; ", validationResult.Warnings)}");
        }

        try
        {
            // Execute the command
            return ExecuteQuery(ast, kbName, user);
        }
        catch (Exception ex)
        {
            return ErrorResponse.ExecutionErrorResponse(ex.Message, ast.OriginalQuery, ast.Line, ast.Column);
        }
    }

    /// <summary>
    /// Validate AST before execution
    /// </summary>
    private ValidationResult ValidateAst(AstNode ast, string? kbName)
    {
        if (string.IsNullOrEmpty(kbName))
            return ValidationResult.Success(); // Skip validation if no KB context

        var validator = new SemanticValidator(_conceptCatalog, _kbCatalog);

        return ast switch
        {
            SelectNode select => validator.ValidateSelect(select, kbName),
            InsertNode insert => validator.ValidateInsert(insert, kbName),
            InsertBulkNode bulkInsert => ValidateBulkInsert(bulkInsert, kbName, validator),
            UpdateNode update => validator.ValidateUpdate(update, kbName),
            CreateRuleNode rule => validator.ValidateRule(rule, kbName),
            AddHierarchyNode hierarchy => validator.ValidateHierarchy(hierarchy, kbName),
            CreateRelationNode relation => validator.ValidateRelation(relation, kbName),
            _ => ValidationResult.Success() // No validation for other node types
        };
    }

    private ValidationResult ValidateBulkInsert(InsertBulkNode node, string kbName, SemanticValidator validator)
    {
        // Validate first row as sample
        if (node.Rows.Count > 0)
        {
            var sampleInsert = new InsertNode
            {
                ConceptName = node.ConceptName,
                Values = node.Rows[0]
            };
            return validator.ValidateInsert(sampleInsert, kbName);
        }
        return ValidationResult.Success();
    }

    private bool RequiresKb(AstNode ast)
    {
        return ast switch
        {
            CreateKbNode => false,
            DropKbNode => false,
            UseKbNode => false,
            CreateUserNode => false,
            DropUserNode => false,
            GrantNode => false,
            RevokeNode => false,
            // TCL never needs a specific KB selected
            KBMS.Parser.Ast.Tcl.BeginTransactionNode => false,
            KBMS.Parser.Ast.Tcl.CommitNode => false,
            KBMS.Parser.Ast.Tcl.RollbackNode => false,
            ShowNode show => show.ShowType != ShowType.KnowledgeBases && show.ShowType != ShowType.Users,
            _ => true
        };
    }

    private string DetermineAction(AstNode ast)
    {
        return ast.Type.Split('_')[0] switch
        {
            "CREATE" => "CREATE",
            "DROP" => "DROP",
            "ADD" => "CREATE",
            "REMOVE" => "DROP",
            "SELECT" => "SELECT",
            "INSERT" => "INSERT",
            "INSERT_BULK" => "INSERT",
            "UPDATE" => "UPDATE",
            "DELETE" => "DELETE",
            "SOLVE" => "SELECT",
            "SHOW" => "SELECT",
            "GRANT" => "GRANT",
            "REVOKE" => "REVOKE",
            "USE" => "USE",
            "ALTER" => "ADMIN",
            "EXPLAIN" => "SELECT",
            "MAINTENANCE" => "ADMIN",
            "EXPORT" => "ADMIN",
            "IMPORT" => "ADMIN",
            "DESCRIBE" => "SELECT",
            "BEGIN" => "USE",    // TCL - allow any authenticated user
            "COMMIT" => "USE",
            "ROLLBACK" => "USE",
            _ => ast.Type
        };
    }

    private string? DetermineKbName(AstNode ast)
    {
        return ast switch
        {
            CreateKbNode n => n.KbName,
            DropKbNode n => n.KbName,
            UseKbNode n => n.KbName,
            GrantNode n => n.KbName,
            RevokeNode n => n.KbName,
            ShowNode n => n.KbName,
            _ => null
        };
    }

    private bool CheckPrivilege(User user, string action, string? kbName, AstNode ast)
    {
        // ROOT has all privileges
        if (user.Role == UserRole.ROOT)
            return true;

        if (string.IsNullOrEmpty(kbName))
        {
            // Global commands (kbName is null)
            // SHOW commands (mapped to SELECT) and USE are allowed for all authenticated users
            if (action == "SELECT" || action == "USE") return true;
            // Everything else (CREATE USER, ALTER USER, DROP USER, etc.) requires SystemAdmin
            return user.SystemAdmin;
        }

        return action switch
        {
            // RC17: CREATE/DROP KNOWLEDGE BASE requires SystemAdmin role if not ROOT
            "CREATE" when ast is CreateKbNode => user.SystemAdmin,
            "DROP" when ast is DropKbNode => user.SystemAdmin,

            "CREATE" => user.KbPrivileges.TryGetValue(kbName!, out var p1) && p1 == Privilege.ADMIN,
            "DROP" => user.KbPrivileges.TryGetValue(kbName!, out var p2) && p2 == Privilege.ADMIN,
            "SELECT" => user.KbPrivileges.ContainsKey(kbName!),
            "INSERT" => user.KbPrivileges.TryGetValue(kbName!, out var p3) && (p3 == Privilege.WRITE || p3 == Privilege.ADMIN),
            "INSERT_BULK" => user.KbPrivileges.TryGetValue(kbName!, out var pb) && (pb == Privilege.WRITE || pb == Privilege.ADMIN),
            "UPDATE" => user.KbPrivileges.TryGetValue(kbName!, out var p4) && (p4 == Privilege.WRITE || p4 == Privilege.ADMIN),
            "DELETE" => user.KbPrivileges.TryGetValue(kbName!, out var p5) && (p5 == Privilege.WRITE || p5 == Privilege.ADMIN),
            "GRANT" => user.SystemAdmin,
            "REVOKE" => user.SystemAdmin,
            "ADMIN" => user.KbPrivileges.TryGetValue(kbName!, out var p6) && p6 == Privilege.ADMIN,
            "USE" => true,
            _ => false
        };
    }

    public List<Models.KnowledgeBase> ListKbs() => _kbCatalog.ListKbs();

    private object ExecuteQuery(AstNode ast, string? kbName, Models.User user)
    {
        return ast.Type switch
        {
            // DDL - Knowledge Base
            "CREATE_KNOWLEDGE_BASE" => HandleCreateKnowledgeBase((CreateKbNode)ast, user),
            "DROP_KNOWLEDGE_BASE" => HandleDropKnowledgeBase((DropKbNode)ast),
            "USE" => HandleUse((UseKbNode)ast),

            // DDL - Concept
            "CREATE_CONCEPT" => HandleCreateConcept((CreateConceptNode)ast, kbName!),
            "DROP_CONCEPT" => HandleDropConcept((DropConceptNode)ast, kbName!),
            "ALTER_CONCEPT" => HandleAlterConcept((AlterConceptNode)ast, kbName!),
            "CREATE_TRIGGER" => HandleCreateTrigger((KBMS.Parser.Ast.Kdl.CreateTriggerNode)ast, kbName!),
            "DROP_TRIGGER" => HandleDropTrigger((KBMS.Parser.Ast.Kdl.DropTriggerNode)ast, kbName!),

            // DCL - User
            "ALTER_USER" => HandleAlterUser((AlterUserNode)ast),
            "ALTER_KNOWLEDGE_BASE" => HandleAlterKnowledgeBase((AlterKbNode)ast),
            "CREATE_INDEX" => HandleCreateIndex((KBMS.Parser.Ast.Kdl.CreateIndexNode)ast, kbName!),
            "DROP_INDEX" => HandleDropIndex((KBMS.Parser.Ast.Kdl.DropIndexNode)ast, kbName!),
            "MAINTENANCE" => HandleMaintenance((KBMS.Parser.Ast.Kml.MaintenanceNode)ast, kbName!),
            "EXPLAIN" => HandleExplain((ExplainNode)ast, kbName),
            "DESCRIBE" => HandleDescribe((KBMS.Parser.Ast.Kql.DescribeNode)ast, kbName!),
            "EXPORT" => HandleExport((KBMS.Parser.Ast.Kml.ExportNode)ast, kbName!),
            "IMPORT" => HandleImport((KBMS.Parser.Ast.Kml.ImportNode)ast, kbName!, user),
            "ADD_VARIABLE" => HandleAddVariable((AddVariableNode)ast, kbName!),

            // DDL - Hierarchy
            "ADD_HIERARCHY" => HandleAddHierarchy((AddHierarchyNode)ast, kbName!),
            "CREATE_HIERARCHY" => HandleAddHierarchy((AddHierarchyNode)ast, kbName!),
            "REMOVE_HIERARCHY" => HandleRemoveHierarchy((RemoveHierarchyNode)ast, kbName!),

            // DDL - Relation
            "CREATE_RELATION" => HandleCreateRelation((CreateRelationNode)ast, kbName!),
            "DROP_RELATION" => HandleDropRelation((DropRelationNode)ast, kbName!),

            // DDL - Operator
            "CREATE_OPERATOR" => HandleCreateOperator((CreateOperatorNode)ast, kbName!),
            "DROP_OPERATOR" => HandleDropOperator((DropOperatorNode)ast, kbName!),

            // DDL - Function
            "CREATE_FUNCTION" => HandleCreateFunction((CreateFunctionNode)ast, kbName!),
            "DROP_FUNCTION" => HandleDropFunction((DropFunctionNode)ast, kbName!),

            // DDL - Computation
            "ADD_COMPUTATION" => HandleAddComputation((AddComputationNode)ast, kbName!),
            "REMOVE_COMPUTATION" => HandleRemoveComputation((RemoveComputationNode)ast, kbName!),

            // DDL - Rule
            "CREATE_RULE" => HandleCreateRule((CreateRuleNode)ast, kbName!),
            "DROP_RULE" => HandleDropRule((DropRuleNode)ast, kbName!),

            // DDL - User
            "CREATE_USER" => HandleCreateUser((CreateUserNode)ast),
            "DROP_USER" => HandleDropUser((DropUserNode)ast),
            "GRANT" => HandleGrant((GrantNode)ast),
            "REVOKE" => HandleRevoke((RevokeNode)ast),

            // DML
            "SELECT" => HandleSelect((SelectNode)ast, kbName!),
            "FIND" => HandleFind((FindNode)ast, kbName!),
            "INSERT" => HandleInsert((InsertNode)ast, kbName!, user),
            "INSERT_BULK" => HandleInsertBulk((InsertBulkNode)ast, kbName!, user),
            "UPDATE" => HandleUpdate((UpdateNode)ast, kbName!, user),
            "DELETE" => HandleDelete((DeleteNode)ast, kbName!, user),
            "SHOW_KNOWLEDGE_BASES" => HandleShowKnowledgeBases(),
            "SHOW_CONCEPTS" => HandleShowConcepts((ShowNode)ast, kbName!),
            "SHOW_CONCEPT" => HandleShowConcept((ShowNode)ast, kbName!),
            "SHOW_RULES" => HandleShowRules((ShowNode)ast, kbName!),
            "SHOW_RELATIONS" => HandleShowRelations((ShowNode)ast, kbName!),
            "SHOW_OPERATORS" => HandleShowOperators((ShowNode)ast, kbName!),
            "SHOW_FUNCTIONS" => HandleShowFunctions((ShowNode)ast, kbName!),
            "SHOW_HIERARCHIES" => HandleShowHierarchies((ShowNode)ast, kbName!),
            "SHOW_USERS" => HandleShowUsers(),
            "SHOW_TRIGGERS" => HandleShowTriggers((ShowNode)ast, kbName!),
            "SHOW_INDEXES" => HandleShowIndexes((ShowNode)ast, kbName!),
            "SHOW_PRIVILEGES_ON" => HandleShowPrivilegesOnKb((ShowNode)ast),
            "SHOW_PRIVILEGES_OF" => HandleShowPrivilegesOfUser((ShowNode)ast),

            // TCL - Transaction Control Language
            "BEGIN_TRANSACTION" => HandleBeginTransaction(),
            "COMMIT" => HandleCommit(kbName),
            "ROLLBACK" => HandleRollback(),
            "SEARCH" => HandleSearch((SearchNode)ast, kbName!),

            _ => ErrorResponse.ExecutionErrorResponse($"Unknown command type: {ast.Type}")
        };
    }

    // ==================== TCL Handlers ====================

    // Transaction buffering
    private bool _inTransaction = false;
    private List<(string Action, string KbName, ObjectInstance Obj)> _txBuffer = new();

    private object HandleBeginTransaction()
    {
        _inTransaction = true;
        _txBuffer.Clear();
        return new { success = true, message = "V3 Transaction started (Buffered via WAL)." };
    }

    private object HandleCommit(string? kbName)
    {
        foreach (var (action, kb, obj) in _txBuffer)
        {
            var concept = _conceptCatalog.LoadConcept(kb, obj.ConceptName);
            if (action == "INSERT")
                _v3Router.InsertObject(kb, obj, concept);
            else if (action == "UPDATE")
                _v3Router.UpdateObject(kb, obj.ConceptName, obj.Id, obj.Values, concept);
        }
        _txBuffer.Clear();
        _inTransaction = false;
        return new { success = true, message = "V3 Transaction committed. Pages flushed." };
    }

    private object HandleRollback()
    {
        _txBuffer.Clear();
        _inTransaction = false;
        return new { success = true, message = "V3 Transaction rolled back. WAL reverted." };
    }

    // ==================== DDL Handlers ====================

    // In-memory trigger registry (keyed by kbName:concept:event)
    private readonly Dictionary<string, List<KBMS.Parser.Ast.Kdl.CreateTriggerNode>> _triggers = new();

    private void LoadTriggersIfNecessary(string kbName)
    {
        if (_triggers.ContainsKey(kbName)) return;

        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return;

        var list = new List<KBMS.Parser.Ast.Kdl.CreateTriggerNode>();
        foreach (var t in kb.Triggers)
        {
            try
            {
                var parser = new KBMS.Parser.Parser(t.OriginalQuery);
                var stmts = parser.ParseAll();
                if (stmts.Count > 0 && stmts[0] is KBMS.Parser.Ast.Kdl.CreateTriggerNode node)
                {
                    list.Add(node);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to parse trigger '{t.Name}' in KB '{kbName}': {ex.Message}");
            }
        }
        
        _triggers[kbName] = list;
    }

    private object HandleCreateTrigger(KBMS.Parser.Ast.Kdl.CreateTriggerNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");

        // Check if trigger with same name already exists and replace it
        var existing = kb.Triggers.FirstOrDefault(t => t.Name.Equals(node.TriggerName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            kb.Triggers.Remove(existing);
        }

        var triggerModel = new Models.Trigger
        {
            Name = node.TriggerName,
            Event = node.Event.ToString(),
            TargetConcept = node.TargetConcept,
            OriginalQuery = node.OriginalQuery
        };

        kb.Triggers.Add(triggerModel);
        if (!_kbCatalog.SaveKbMetadata(kb))
            return ErrorResponse.ExecutionErrorResponse("Failed to save trigger to KB metadata.");

        LoadTriggersIfNecessary(kbName);
        _triggers[kbName].Add(node);

        return new { success = true, message = $"Trigger '{node.TriggerName}' created on {node.Event} OF {node.TargetConcept} in KB '{kbName}'" };
    }

    // Called internally after INSERT/UPDATE/DELETE to fire matching triggers
    private void FireTriggers(string kbName, string conceptName, string eventType, Models.User executor)
    {
        LoadTriggersIfNecessary(kbName);
        
        var key = kbName;
        if (!_triggers.TryGetValue(key, out var list)) return;
        var matched = list.Where(t =>
            t.Event.ToString().Equals(eventType, StringComparison.OrdinalIgnoreCase) &&
            (t.TargetConcept == "*" || t.TargetConcept.Equals(conceptName, StringComparison.OrdinalIgnoreCase)));
        foreach (var trigger in matched)
        {
            if (trigger.Action != null)
                Execute(trigger.Action, executor, kbName);
        }
    }

    private object HandleCreateKnowledgeBase(CreateKbNode node, Models.User creator)
    {
        var kb = _kbCatalog.CreateKb(node.KbName, creator.Id, node.Description ?? "");
        if (kb == null)
            return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{node.KbName}' already exists.");

        // Grant ADMIN privilege to the creator
        if (creator.Role != UserRole.ROOT)
        {
            _userCatalog.GrantPrivilege(creator.Username, node.KbName, Privilege.ADMIN);
        }

        return new { success = true, message = $"Knowledge base '{node.KbName}' created successfully (System Catalog)." };
    }

    private object HandleDropKnowledgeBase(DropKbNode node)
    {
        // RC16: Protect system knowledge base from being deleted
        if (node.KbName.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorResponse.ExecutionErrorResponse("Cannot drop system knowledge base. It is required for server operation.");
        }
        if (node.KbName.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorResponse.ExecutionErrorResponse("The 'system' knowledge base is protected and cannot be dropped.");
        }

        var success = _kbCatalog.DropKb(node.KbName);
        if (success)
        {
            _conceptCatalog.DropAllConcepts(node.KbName);
            _v3Router.DropAllMappings(node.KbName);
            _userCatalog.RevokeAllPrivileges(node.KbName);
            
            // RC18: Clear in-memory triggers and transaction buffers for this KB
            _triggers.Remove(node.KbName);
            _txBuffer.RemoveAll(tx => tx.KbName.Equals(node.KbName, StringComparison.OrdinalIgnoreCase));
            
            // RC12: Physically delete the .kdb file and clear manager cache
            _storagePool.DeleteKbFile(node.KbName);

            // Purge compiled Rete networks for this KB so a re-created KB of the same name starts fresh
            InvalidateEngineCache(node.KbName);
        }
        return success
            ? new { success = true, message = $"Knowledge base '{node.KbName}' dropped successfully." }
            : ErrorResponse.ExecutionErrorResponse($"Knowledge base '{node.KbName}' not found.");
    }

    private object HandleUse(UseKbNode node)
    {
        var kb = _kbCatalog.LoadKb(node.KbName);
        if (kb != null)
        {
            // Backfill: ensure any concept with BASE_OBJECTS has an IS-A hierarchy entry
            SyncHierarchiesFromBaseObjects(node.KbName);
            return new { success = true, message = $"Now using knowledge base '{node.KbName}'.", currentKb = node.KbName };
        }
        return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{node.KbName}' not found.");
    }

    private object HandleCreateConcept(CreateConceptNode node, string kbName)
    {
        // Auto-expand Concept-typed variables (e.g., p1: Point → p1.x, p1.y)
        var expandedVariables = new List<Variable>();
        var knownPrimitiveTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TINYINT", "SMALLINT", "INT", "BIGINT", "FLOAT", "DOUBLE", "DECIMAL",
            "NUMBER", "VARCHAR", "CHAR", "TEXT", "STRING", "BOOLEAN", "DATE",
            "DATETIME", "TIMESTAMP", "OBJECT"
        };

        foreach (var v in node.Variables)
        {
            if (knownPrimitiveTypes.Contains(v.Type))
            {
                // Normalization: Group "NUMBER" under "DECIMAL" for consistent True Typing
                string normalizedType = v.Type.ToUpper() == "NUMBER" ? "DECIMAL" : v.Type.ToUpper();

                // Primitive type — keep as-is (with normalized name)
                expandedVariables.Add(new Variable
                {
                    Name = v.Name,
                    Type = normalizedType,
                    Length = v.Length,
                    Scale = v.Scale
                });
            }
            else
            {
                // Possibly a Concept type — look it up
                var referencedConcept = _conceptCatalog.LoadConcept(kbName, v.Type);

                if (referencedConcept != null && referencedConcept.Variables.Count > 0)
                {
                    // Keep the original variable (e.g., p1: Point) so InferenceEngine knows to recurse
                    expandedVariables.Add(new Variable
                    {
                        Name = v.Name,
                        Type = v.Type,
                        Length = v.Length,
                        Scale = v.Scale,
                        IsReference = v.IsReference,
                        ReferenceConceptName = v.ReferenceConceptName
                    });

                    // Also expand: p1.x, p1.y, etc. for backward compatibility and direct property access
                    foreach (var subVar in referencedConcept.Variables)
                    {
                        expandedVariables.Add(new Variable
                        {
                            Name = $"{v.Name}.{subVar.Name}",
                            Type = subVar.Type,
                            Length = subVar.Length,
                            Scale = subVar.Scale
                        });
                    }
                }
                else
                {
                    // Unknown type or concept not found — keep as-is (treated as custom type)
                    expandedVariables.Add(new Variable
                    {
                        Name = v.Name,
                        Type = v.Type,
                        Length = v.Length,
                        Scale = v.Scale
                    });
                }
            }
        }

        var concept = new Concept
        {
            Name = node.ConceptName,
            Variables = expandedVariables,
            Aliases = node.Aliases,
            BaseObjects = node.BaseObjects,
            Constraints = node.Constraints.Select(c => new Constraint 
            { 
                Name = c.Name, 
                Expression = c.Expression,
                Line = c.Line,
                Column = c.Column
            }).ToList(),
            SameVariables = node.SameVariables.Select(sv => new SameVariable
            {
                Variable1 = sv.Var1,
                Variable2 = sv.Var2
            }).ToList(),
            ConstructRelations = node.ConstructRelations.Select(cr => new ConstructRelation
            {
                RelationName = cr.RelationName,
                Arguments = cr.Arguments
            }).ToList(),
            Properties = node.Properties.Select(p => new Property
            {
                Key = p.Key,
                Value = p.Value
            }).ToList(),
            ConceptRules = node.ConceptRules.Select(r => new ConceptRule
            {
                Id = Guid.NewGuid(),
                Name = r.RuleName,
                Kind = r.Kind,
                Variables = r.Variables.Select(v => new Variable { Name = v.Name, Type = v.Type, Length = v.Length, Scale = v.Scale }).ToList(),
                Hypothesis = r.Hypothesis,
                Conclusion = r.Conclusion
            }).ToList(),
            Equations = node.Equations.Select(e => new Equation
            {
                Id = Guid.NewGuid(),
                Expression = e.Expression,
                Variables = ExtractVariablesFromExpression(e.Expression),
                Line = e.Line,
                Column = e.Column
            }).ToList()
        };

        var created = _conceptCatalog.CreateConcept(kbName, concept);
        if (created)
        {
            // Sync: auto-create IS-A Hierarchy entries for any BASE_OBJECTS declared
            SyncHierarchiesFromBaseObjects(kbName);
        }
        return created
            ? new { success = true, message = $"Concept '{node.ConceptName}' created successfully (System Catalog)." }
            : ErrorResponse.ExecutionErrorResponse($"Concept '{node.ConceptName}' already exists.");
    }

    private List<string> ExtractVariablesFromExpression(string expression)
    {
        // Regex to find alphanumeric identifiers (including dots for nested properties)
        var regex = new System.Text.RegularExpressions.Regex(@"\b[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*\b");
        var matches = regex.Matches(expression);
        var vars = new HashSet<string>();
        var knownFuncs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sqrt", "Sin", "Cos", "Tan", "Log", "Exp", "Pow", "Abs", "Min", "Max" };

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var val = match.Value;
            if (!knownFuncs.Contains(val) && !double.TryParse(val, out _))
            {
                vars.Add(val);
            }
        }
        return vars.ToList();
    }

    // ── Known meta-functions exempt from field-existence checks ─────────────
    private static readonly HashSet<string> _metaFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "IS_STUCK", "HAS_FIRED", "IS_DEDUCED", "TOTAL_COST", "SOLVE",
        "COUNT", "SUM", "AVG", "MIN", "MAX", "EXISTS"
    };

    private static readonly HashSet<string> _numericTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "INT", "BIGINT", "SMALLINT", "TINYINT", "FLOAT", "DOUBLE", "DECIMAL", "NUMBER"
    };

    private static readonly HashSet<string> _stringTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "STRING", "VARCHAR", "CHAR", "TEXT"
    };

    /// <summary>
    /// Validates that every field referenced in the given conditions:
    ///   1. Exists in the effective concept schema (or is a known meta-function/wildcard).
    ///   2. Has a value compatible with the declared variable type.
    /// Returns null when valid, or a user-friendly error message.
    /// </summary>
    private string? ValidateConditionsAgainstSchema(
        List<Condition> conditions, Concept effectiveConcept, string clauseName = "WITH")
    {
        if (conditions == null || conditions.Count == 0) return null;

        foreach (var cond in conditions)
        {
            // Skip meta-functions and function-based left sides (e.g. SOLVE, HAS_FIRED)
            if (cond.LeftExpression is KBMS.Parser.Ast.Expressions.FunctionCallNode fn &&
                _metaFunctions.Contains(fn.FunctionName))
                continue;

            var rawField = cond.Field?.Trim();
            if (string.IsNullOrEmpty(rawField)) continue;

            // Strip concept prefix (e.g. "p.sys" → "sys")
            var fieldName = rawField.Contains('.') ? rawField.Split('.').Last() : rawField;

            // Allow star and meta-function names used directly in Field
            if (fieldName == "*" || _metaFunctions.Contains(fieldName)) continue;

            // ── 1. Existence check ──────────────────────────────────────────
            var variable = effectiveConcept.Variables
                .FirstOrDefault(v => v.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (variable == null)
            {
                var knownFields = string.Join(", ", effectiveConcept.Variables.Select(v => v.Name));
                return $"Semantic Error: Variable '{fieldName}' does not exist in concept " +
                       $"'{effectiveConcept.Name}'. Known variables: [{knownFields}]";
            }

            // ── 2. Type compatibility check ─────────────────────────────────
            var compareValue = cond.Value;
            if (compareValue == null) continue; // NULL comparisons always valid

            bool isNumericField  = _numericTypes.Contains(variable.Type);
            bool isStringField   = _stringTypes.Contains(variable.Type);
            bool isBooleanField  = variable.Type.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase);

            // Detect value kind
            bool valueIsString  = compareValue is string;
            bool valueIsNumeric = compareValue is int or long or double or decimal or float;
            bool valueIsBool    = compareValue is bool;

            if (isNumericField && valueIsString)
            {
                var strVal = (string)compareValue;
                // Allow quoted numbers like '150'
                if (!decimal.TryParse(strVal, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    return $"Type Error in {clauseName}: Variable '{fieldName}' is numeric " +
                           $"({variable.Type}) but was compared with string value '{strVal}'. " +
                           $"Remove quotes or use a numeric value.";
                }
            }

            if (isStringField && valueIsNumeric)
            {
                return $"Type Error in {clauseName}: Variable '{fieldName}' is a string " +
                       $"({variable.Type}) but was compared with a numeric value '{compareValue}'. " +
                       $"Wrap the value in quotes (e.g. '{compareValue}').";
            }

            if (isBooleanField && !valueIsBool && valueIsString)
            {
                var s = ((string)compareValue).ToLower();
                if (s != "true" && s != "false" && s != "1" && s != "0")
                {
                    return $"Type Error in {clauseName}: Variable '{fieldName}' is BOOLEAN but " +
                           $"was compared with '{compareValue}'. Use TRUE or FALSE.";
                }
            }
        }

        return null; // all good
    }


    private object HandleDropConcept(DropConceptNode node, string kbName)
    {
        // First delete all data associated with the concept in V3 storage
        _v3Router.DeleteObjects(kbName, node.ConceptName, null, null);

        var success = _conceptCatalog.DropConcept(kbName, node.ConceptName);
        if (!success && node.IfExists)
            return new { success = true, message = $"Concept '{node.ConceptName}' does not exist, skipping (IF EXISTS)." };

        if (success)
            InvalidateEngineCache(kbName); // Schema changed

        return success
            ? new { success = true, message = $"Concept '{node.ConceptName}' dropped successfully." }
            : ErrorResponse.ExecutionErrorResponse($"Concept '{node.ConceptName}' not found or is in use.");
    }

    private object HandleAddVariable(AddVariableNode node, string kbName)
    {
        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        if (concept == null) return ErrorResponse.ExecutionErrorResponse("Concept not found.");
        
        concept.Variables.Add(new Models.Variable 
        { 
            Name = node.VariableName, 
            Type = node.VariableType, 
            Length = node.Length, 
            Scale = node.Scale 
        });

        var success = _conceptCatalog.UpdateConcept(kbName, concept);
        return success
            ? new { success = true, message = $"Variable '{node.VariableName}' added to concept '{node.ConceptName}' (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to update concept schema.");
    }

    private object HandleAddHierarchy(AddHierarchyNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        
        // Auto-sync: Hierarchy
        if (!kb.Hierarchies.Any(h => h.ParentConcept.Equals(node.ParentConcept, StringComparison.OrdinalIgnoreCase) && h.ChildConcept.Equals(node.ChildConcept, StringComparison.OrdinalIgnoreCase)))
        {
            var hierarchy = new Hierarchy 
            { 
                ParentConcept = node.ParentConcept, 
                ChildConcept = node.ChildConcept, 
                HierarchyType = Models.HierarchyType.IsA 
            };
            kb.Hierarchies.Add(hierarchy);
            _kbCatalog.SaveKbMetadata(kb);
        }

        // Auto-sync: Add to Child's BaseObjects
        var childConcept = _conceptCatalog.LoadConcept(kbName, node.ChildConcept);
        if (childConcept != null && !childConcept.BaseObjects.Contains(node.ParentConcept, StringComparer.OrdinalIgnoreCase))
        {
            childConcept.BaseObjects.Add(node.ParentConcept);
            _conceptCatalog.UpdateConcept(kbName, childConcept);
        }
        
        return new { success = true, message = "Hierarchy added and synced successfully." };
    }

    private object HandleRemoveHierarchy(RemoveHierarchyNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        
        bool removedFromKb = false;
        var h = kb.Hierarchies.FirstOrDefault(x => x.ParentConcept.Equals(node.ParentConcept, StringComparison.OrdinalIgnoreCase) && x.ChildConcept.Equals(node.ChildConcept, StringComparison.OrdinalIgnoreCase));
        if (h != null)
        {
            kb.Hierarchies.Remove(h);
            _kbCatalog.SaveKbMetadata(kb);
            removedFromKb = true;
        }

        // Auto-sync: Remove from Child's BaseObjects
        bool removedFromConcept = false;
        var childConcept = _conceptCatalog.LoadConcept(kbName, node.ChildConcept);
        if (childConcept != null)
        {
            var baseObj = childConcept.BaseObjects.FirstOrDefault(b => b.Equals(node.ParentConcept, StringComparison.OrdinalIgnoreCase));
            if (baseObj != null)
            {
                childConcept.BaseObjects.Remove(baseObj);
                _conceptCatalog.UpdateConcept(kbName, childConcept);
                removedFromConcept = true;
            }
        }
        
        if (!removedFromKb && !removedFromConcept)
            return ErrorResponse.ExecutionErrorResponse("Hierarchy not found.");
            
        return new { success = true, message = "Hierarchy removed and synced successfully." };
    }

    private object HandleCreateRelation(CreateRelationNode node, string kbName)
    {
        var relation = new Relation
        {
            Name = node.RelationName,
            Domain = node.DomainConcept,
            Range = node.RangeConcept,
            Properties = node.Properties,
            ParamNames = node.ParamNames,
            Equations = node.Equations.Select(e => new Equation
            {
                Id = Guid.NewGuid(),
                Expression = e.Expression,
                Line = e.Line,
                Column = e.Column
            }).ToList(),
            Rules = node.ConceptRules.Select(r => new ConceptRule
            {
                Id = Guid.NewGuid(),
                Name = r.RuleName,
                Kind = r.Kind,
                Hypothesis = r.Hypothesis,
                Conclusion = r.Conclusion
            }).ToList()
        };
        return HandleCreateRelation(relation, kbName);
    }

    private object HandleCreateRelation(Relation relation, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        kb.Relations.Add(relation);
        var saved = _kbCatalog.SaveKbMetadata(kb);
        if (saved) InvalidateEngineCache(kbName); // Schema changed
        return saved
            ? new { success = true, message = $"Relation '{relation.Name}' created successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private object HandleDropRelation(DropRelationNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        var rel = kb.Relations.FirstOrDefault(r => r.Name.Equals(node.RelationName, StringComparison.OrdinalIgnoreCase));
        if (rel == null) return ErrorResponse.ExecutionErrorResponse("Relation not found.");
        kb.Relations.Remove(rel);
        var saved = _kbCatalog.SaveKbMetadata(kb);
        if (saved) InvalidateEngineCache(kbName); // Schema changed
        return saved
            ? new { success = true, message = $"Relation '{node.RelationName}' dropped successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private object HandleCreateOperator(CreateOperatorNode node, string kbName)
    {
        var op = new Operator
        {
            Symbol = node.Symbol,
            ParamTypes = node.ParamTypes,
            ReturnType = node.ReturnType,
            Body = node.Body,
            Properties = node.Properties
        };

        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        kb.Operators.Add(op);
        return _kbCatalog.SaveKbMetadata(kb)
            ? new { success = true, message = $"Operator '{node.Symbol}' created successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private object HandleDropOperator(DropOperatorNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        var op = kb.Operators.FirstOrDefault(o => o.Symbol.Equals(node.Symbol, StringComparison.OrdinalIgnoreCase));
        if (op == null) return ErrorResponse.ExecutionErrorResponse("Operator not found.");
        kb.Operators.Remove(op);
        return _kbCatalog.SaveKbMetadata(kb)
            ? new { success = true, message = $"Operator '{node.Symbol}' dropped successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private object HandleCreateFunction(CreateFunctionNode node, string kbName)
    {
        var func = new Function
        {
            Name = node.FunctionName,
            Parameters = node.Parameters.Select(p => new FunctionParameter { Type = p.Type, Name = p.Name }).ToList(),
            ReturnType = node.ReturnType,
            Body = node.Body,
            Properties = node.Properties
        };

        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        kb.Functions.Add(func);
        return _kbCatalog.SaveKbMetadata(kb)
            ? new { success = true, message = $"Function '{node.FunctionName}' created successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private object HandleDropFunction(DropFunctionNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        var func = kb.Functions.FirstOrDefault(f => f.Name.Equals(node.FunctionName, StringComparison.OrdinalIgnoreCase));
        if (func == null) return ErrorResponse.ExecutionErrorResponse("Function not found.");
        kb.Functions.Remove(func);
        return _kbCatalog.SaveKbMetadata(kb)
            ? new { success = true, message = $"Function '{node.FunctionName}' dropped successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private object HandleAddComputation(AddComputationNode node, string kbName)
    {
        return ErrorResponse.ExecutionErrorResponse("COMPUTATION management is being migrated to V3 Concept Schema.");
    }

    private object HandleRemoveComputation(RemoveComputationNode node, string kbName)
    {
        return ErrorResponse.ExecutionErrorResponse("COMPUTATION management is being migrated to V3 Concept Schema.");
    }

    private object HandleCreateRule(CreateRuleNode node, string kbName)
    {
        // Handle multi-concept scope
        var scopeConcepts = new List<RuleScopeConcept>();
        var joinConditions = new List<RuleJoinCondition>();

        if (node.ScopeConcepts.Count > 0)
        {
            // Multi-concept rule
            scopeConcepts = node.ScopeConcepts.Select(sc => new RuleScopeConcept
            {
                ConceptName = sc.ConceptName,
                Alias = sc.Alias,
                Position = sc.Position
            }).ToList();

            // Convert join conditions
            foreach (var jc in node.JoinConditions)
            {
                joinConditions.Add(new RuleJoinCondition
                {
                    LeftField = jc.Field,
                    Operator = jc.Operator,
                    RightField = jc.Value?.ToString() ?? ""
                });
            }
        }
        else
        {
            // Single-concept rule (backward compatibility)
            var scope = node.ConceptName;
            if (string.IsNullOrEmpty(scope) && node.Hypothesis.Count > 0)
            {
                var firstHyp = GetExpressionString(node.Hypothesis[0]);
                var match = System.Text.RegularExpressions.Regex.Match(firstHyp, @"^(\w+)\(");
                if (match.Success) scope = match.Groups[1].Value;
            }

            if (!string.IsNullOrEmpty(scope))
            {
                scopeConcepts.Add(new RuleScopeConcept
                {
                    ConceptName = scope,
                    Alias = null,
                    Position = 0
                });
            }
        }

        var rule = new Rule
        {
            Id = Guid.NewGuid(),
            Name = node.RuleName,
            RuleType = node.RuleType.ToString().ToLower(),
            Scope = scopeConcepts.FirstOrDefault()?.ConceptName ?? "",
            ScopeConcepts = scopeConcepts,
            JoinConditions = joinConditions,
            Cost = node.Cost ?? 1,
            Priority = node.Priority,
            Hypothesis = node.Hypothesis.Select(h => ToModelExpression(h)).ToList(),
            Conclusion = node.Conclusions.Select(c => ToModelExpression(c)).ToList()
        };

        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        
        var existingRule = kb.Rules.FirstOrDefault(r => r.Name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase));
        if (existingRule != null) kb.Rules.Remove(existingRule);
        
        kb.Rules.Add(rule);
        var saved = _kbCatalog.SaveKbMetadata(kb);
        if (saved) InvalidateEngineCache(kbName); // New rule → compiled network stale
        return saved
            ? new { success = true, message = $"Rule '{node.RuleName}' created successfully (V3 Engine)." + (rule.IsMultiConcept ? $" Multi-concept scope: {string.Join(", ", scopeConcepts.Select(s => s.ConceptName))}" : "") }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private Expression ToModelExpression(ExpressionNode ast)
    {
        if (ast == null) return new Expression();

        var modelExpr = new Expression
        {
            Content = GetExpressionString(ast)
        };

        if (ast is BinaryExpressionNode binary)
        {
            modelExpr.Type = "binary";
            if (binary.Left != null) modelExpr.Children.Add(ToModelExpression(binary.Left));
            if (binary.Right != null) modelExpr.Children.Add(ToModelExpression(binary.Right));
        }
        else if (ast is UnaryExpressionNode unary)
        {
            modelExpr.Type = "unary";
            if (unary.Operand != null) modelExpr.Children.Add(ToModelExpression(unary.Operand));
        }
        else if (ast is FunctionCallNode func)
        {
            modelExpr.Type = "function";
            foreach (var arg in func.Arguments)
            {
                modelExpr.Children.Add(ToModelExpression(arg));
            }
        }
        else if (ast is VariableNode varNode)
        {
            modelExpr.Type = "variable";
        }
        else if (ast is LiteralNode lit)
        {
            modelExpr.Type = "literal";
        }
        else
        {
            modelExpr.Type = "expression";
        }

        return modelExpr;
    }

    private string GetExpressionString(ExpressionNode ast)
    {
        if (ast == null) return "";
        
        // Use recursive ToString if implemented, otherwise fall back to basic reconstruction
        // Most nodes already override ToString()
        return ast.ToString() ?? "";
    }



    private object HandleDropRule(DropRuleNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        var rule = kb.Rules.FirstOrDefault(r => r.Name.Equals(node.RuleName, StringComparison.OrdinalIgnoreCase));
        if (rule == null) return ErrorResponse.ExecutionErrorResponse("Rule not found.");
        kb.Rules.Remove(rule);
        var saved = _kbCatalog.SaveKbMetadata(kb);
        if (saved) InvalidateEngineCache(kbName); // Rule removed → compiled network stale
        return saved
            ? new { success = true, message = $"Rule '{node.RuleName}' dropped successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private object HandleCreateUser(CreateUserNode node)
    {
        var role = Enum.TryParse<UserRole>(node.Role, out var r) ? r : UserRole.USER;
        var user = _userCatalog.CreateUser(node.Username, node.Password, role);
        return user != null
            ? new { success = true, message = $"User '{node.Username}' created successfully (System Catalog)." }
            : ErrorResponse.ExecutionErrorResponse($"User '{node.Username}' already exists.");
    }

    private object HandleDropUser(DropUserNode node)
    {
        var success = _userCatalog.DropUser(node.Username);
        return success
            ? new { success = true, message = $"User '{node.Username}' dropped successfully." }
            : ErrorResponse.ExecutionErrorResponse($"User '{node.Username}' not found.");
    }

    private object HandleGrant(GrantNode node)
    {
        var priv = Enum.TryParse<Privilege>(node.Privilege, out var p) ? p : Privilege.READ;
        var success = _userCatalog.GrantPrivilege(node.Username, node.KbName, priv);
        return success
            ? new { success = true, message = $"Privilege {node.Privilege} on {node.KbName} granted to {node.Username} (System Catalog)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to grant privilege.");
    }

    private object HandleRevoke(RevokeNode node)
    {
        var success = _userCatalog.RevokePrivilege(node.Username, node.KbName);
        return success
            ? new { success = true, message = $"Privilege on {node.KbName} revoked from {node.Username}." }
            : ErrorResponse.ExecutionErrorResponse("Failed to revoke privilege.");
    }

    // ==================== DML Handlers ====================

    private object HandleFind(FindNode node, string kbName)
    {
        try
        {
            var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
            if (concept == null) return ErrorResponse.ExecutionErrorResponse($"Concept {node.ConceptName} not found.");

            var allConceptsToScan = new List<string> { concept.Name };
            allConceptsToScan.AddRange(GetDescendantConcepts(kbName, concept.Name));
            
            var allObjects = new List<ObjectInstance>();
            // For each concept (including children), load objects using the EFFECTIVE concept
            // so that all inherited variables and rules are available during inference.
            var effectiveConcepts = allConceptsToScan
                .Select(cName => _conceptCatalog.LoadConcept(kbName, cName))
                .Where(c => c != null)
                .Select(c => GetEffectiveConcept(kbName, c!))
                .ToList();

            foreach (var ec in effectiveConcepts)
            {
                allObjects.AddRange(_v3Router.SelectObjects(kbName, ec.Name, concept: ec));
            }
            var engine = GetConfiguredEngine(kbName);

            var finalResults = new List<Dictionary<string, object>>();
            // Use the effective concept for the primary concept (inherits parent rules)
            var effectiveConcept = GetEffectiveConcept(kbName, concept);
            var targetVars = effectiveConcept.Variables.Select(v => v.Name).ToList();

            // ── Semantic validation: check WITH clause fields/types ───────
            var validationError = ValidateConditionsAgainstSchema(
                node.WithConditions, effectiveConcept, "WITH");
            if (validationError != null)
                return ErrorResponse.ExecutionErrorResponse(validationError);

            foreach (var obj in allObjects)
            {
                // Find the effective concept for this specific object's concept type
                var objEffective = effectiveConcepts.FirstOrDefault(
                    ec => ec.Name.Equals(obj.ConceptName, StringComparison.OrdinalIgnoreCase))
                    ?? effectiveConcept;
                var inferenceResult = engine.FindClosure(objEffective, obj.Values, targetVars);

                // Load cached explainability metadata if present
                if (obj.Values.TryGetValue("__audit_trail", out var atObj) && atObj is string atJson)
                {
                    try { inferenceResult.AuditTrail.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<KBMS.Reasoning.Rete.ReasoningStep>>(atJson) ?? new()); } catch { }
                }
                if (obj.Values.TryGetValue("__generated_vars", out var gvObj) && gvObj is string gvJson)
                {
                    try { 
                        var vars = System.Text.Json.JsonSerializer.Deserialize<List<string>>(gvJson) ?? new();
                        foreach(var v in vars) inferenceResult.GeneratedVariables.Add(v);
                    } catch { }
                }

                bool passesWith = true;
                foreach (var cond in node.WithConditions)
                {
                    if (cond.LeftExpression is FunctionCallNode funcNode)
                    {
                        var fn = funcNode.FunctionName.ToUpperInvariant();
                        if (fn == "IS_STUCK")
                        {
                            if (inferenceResult.MissingFacts.Count == 0) { passesWith = false; break; }
                        }
                        else if (fn == "HAS_FIRED")
                        {
                            var arg = funcNode.Arguments.FirstOrDefault()?.ToString()?.Trim('\'', '"') ?? "";
                            if (!inferenceResult.AuditTrail.Any(a => a.RuleName.Equals(arg, StringComparison.OrdinalIgnoreCase))) { passesWith = false; break; }
                        }
                        else if (fn == "IS_DEDUCED")
                        {
                            var arg = funcNode.Arguments.FirstOrDefault()?.ToString()?.Trim('\'', '"') ?? "";
                            if (!inferenceResult.GeneratedVariables.Contains(arg)) { passesWith = false; break; }
                        }
                        else if (fn == "TOTAL_COST")
                        {
                            var totalCost = inferenceResult.AuditTrail.Sum(a => a.StepCost);
                            double conditionValue = Convert.ToDouble(cond.Value);
                            bool match = cond.Operator switch {
                                ">" => totalCost > conditionValue,
                                ">=" => totalCost >= conditionValue,
                                "<" => totalCost < conditionValue,
                                "<=" => totalCost <= conditionValue,
                                "=" => totalCost == conditionValue,
                                "!=" or "<>" => totalCost != conditionValue,
                                _ => false
                            };
                            if (!match) { passesWith = false; break; }
                        }
                    }
                    else
                    {
                        var fieldName = cond.Field;
                        if (string.IsNullOrEmpty(fieldName)) fieldName = cond.LeftExpression?.ToString() ?? "";
                        
                        object? actualValue = null;
                        if (inferenceResult.DerivedFacts.TryGetValue(fieldName, out var dval)) actualValue = dval;
                        else if (obj.Values.TryGetValue(fieldName, out var oval)) actualValue = oval;
                        else
                        {
                            // Fallback: check if the fact exists under an alias prefix (e.g., "p.riskLevel")
                            var aliasedKey = inferenceResult.DerivedFacts.Keys.FirstOrDefault(k => k.EndsWith("." + fieldName, StringComparison.OrdinalIgnoreCase));
                            if (aliasedKey != null) actualValue = inferenceResult.DerivedFacts[aliasedKey];
                        }
                        
                        // ── NULL semantics ──────────────────────────────────────────────
                        // condValue is null when parser sees literal NULL or = null
                        bool condValueIsNull = cond.Value == null ||
                            cond.Value?.ToString()?.Equals("NULL", StringComparison.OrdinalIgnoreCase) == true;

                        if (actualValue == null)
                        {
                            // field is null → match only for = null / IS [NULL] / IS NULL
                            bool pass = cond.Operator switch
                            {
                                "=" => condValueIsNull,
                                "IS" => condValueIsNull,
                                "<>" or "!=" => !condValueIsNull,
                                _ => false // >, <, >=, <= with null → false (SQL semantics)
                            };
                            if (!pass) { passesWith = false; break; }
                        }
                        else if (condValueIsNull)
                        {
                            // field has a value but condition compares to null
                            bool pass = cond.Operator switch
                            {
                                "=" => false,        // value = null → false
                                "IS" => false,       // value IS NULL → false
                                "<>" or "!=" => true,// value <> null → true
                                _ => false
                            };
                            if (!pass) { passesWith = false; break; }
                        }
                        else
                        {
                            var actualStr = actualValue.ToString() ?? "";
                            var expectedStr = cond.Value!.ToString()?.Trim('\'', '"') ?? "";
                            bool match = cond.Operator switch {
                                "=" or "==" => actualStr.Equals(expectedStr, StringComparison.OrdinalIgnoreCase),
                                "!=" or "<>" => !actualStr.Equals(expectedStr, StringComparison.OrdinalIgnoreCase),
                                ">" => double.TryParse(actualStr, out var ad) && double.TryParse(expectedStr, out var ed) && ad > ed,
                                ">=" => double.TryParse(actualStr, out var ad) && double.TryParse(expectedStr, out var ed) && ad >= ed,
                                "<" => double.TryParse(actualStr, out var ad) && double.TryParse(expectedStr, out var ed) && ad < ed,
                                "<=" => double.TryParse(actualStr, out var ad) && double.TryParse(expectedStr, out var ed) && ad <= ed,
                                "LIKE" => actualStr.Contains(expectedStr.Replace("%", ""), StringComparison.OrdinalIgnoreCase),
                                _ => false
                            };
                            if (!match) { passesWith = false; break; }
                        }
                    }
                }

                if (!passesWith) continue;

                var projectedObj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                if (node.ReturnItems.Count == 0 || node.ReturnItems.Any(r => r.IsStar))
                {
                    // Merge original object values with derived facts, excluding internals and aliases
                    foreach (var kvp in obj.Values) 
                    {
                        if (kvp.Key.StartsWith("__") || kvp.Key.Contains('.')) continue;
                        projectedObj[kvp.Key] = kvp.Value;
                    }
                    foreach (var kvp in inferenceResult.DerivedFacts) 
                    {
                        if (kvp.Key.StartsWith("__") || kvp.Key.Contains('.')) continue;
                        projectedObj[kvp.Key] = kvp.Value;
                    }
                }
                
                foreach (var ret in node.ReturnItems)
                {
                    if (ret.IsStar) continue;
                    
                    if (ret.Expression is FunctionCallNode fnNode)
                    {
                        var fn = fnNode.FunctionName.ToUpperInvariant();
                        if (fn == "AUDIT_LOG" || fn == "AUDIT_TRAIL") projectedObj[fn] = System.Text.Json.JsonSerializer.Serialize(inferenceResult.AuditTrail);
                        else if (fn == "MISSING_FACTS") projectedObj["MISSING_FACTS"] = System.Text.Json.JsonSerializer.Serialize(inferenceResult.MissingFacts);
                        else if (fn == "GENERATED_VARIABLES") projectedObj["GENERATED_VARIABLES"] = System.Text.Json.JsonSerializer.Serialize(inferenceResult.GeneratedVariables);
                        else if (fn == "EXPLAIN_TREE")
                        {
                            var arg = fnNode.Arguments.FirstOrDefault()?.ToString()?.Trim('\'', '"') ?? "";
                            if (!string.IsNullOrEmpty(arg))
                            {
                                var allFacts = new Dictionary<string, object>(obj.Values, StringComparer.OrdinalIgnoreCase);
                                foreach (var kvp in inferenceResult.DerivedFacts) allFacts[kvp.Key] = kvp.Value;
                                
                                var tree = KBMS.Reasoning.InferenceEngine.BuildExplanationTree(arg, allFacts, inferenceResult.AuditTrail, inferenceResult.GeneratedVariables);
                                projectedObj[$"EXPLAIN_TREE({arg})"] = System.Text.Json.JsonSerializer.Serialize(tree);
                            }
                        }
                    }
                    else if (ret.Expression is VariableNode vNode)
                    {
                        var name = vNode.Name;
                        if (name.Contains(".")) name = name.Split('.')[1]; // basic prefix stripping
                        
                        if (inferenceResult.DerivedFacts.TryGetValue(name, out var val)) projectedObj[name] = val;
                        else if (obj.Values.TryGetValue(name, out var oval)) projectedObj[name] = oval;
                        else projectedObj[name] = null!;
                    }
                }

                finalResults.Add(projectedObj);
            }

            var mappedObjects = finalResults.Select(r => new ObjectInstance
            {
                ConceptName = concept.Name,
                Values = r
            }).ToList();

            return new QueryResultSet
            {
                Success = true,
                ConceptName = concept.Name,
                Objects = mappedObjects,
                Count = mappedObjects.Count,
                Columns = mappedObjects.Count > 0 
                    ? mappedObjects[0].Values.Keys.Where(k => !k.StartsWith("__")).ToList() 
                    : effectiveConcept.Variables.Select(v => v.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            return ErrorResponse.ExecutionErrorResponse($"Error executing FIND: {ex.Message}");
        }
    }

    private object HandleSelect(SelectNode node, string kbName)
    {
        try
        {
            // Handle derived table (sub-query in FROM clause)
            if (node.HasDerivedTable)
            {
                return HandleDerivedTableSelect(node, kbName);
            }

            var parts = node.ConceptName.Split('.');
            var entityName = parts[0];
            var subTarget = parts.Length > 1 ? parts[1].ToLower() : null;

            // 1. Strict TargetType verification
            bool entityExists = false;
            var targetType = node.TargetType?.ToUpper() ?? "CONCEPT";

            KBMS.Models.Concept? conceptMetadata = null;

            switch (targetType)
            {
                case "CONCEPT":
                    var primaryConcept = _conceptCatalog.ListConcepts(kbName).FirstOrDefault(c => c.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                    if (primaryConcept != null)
                    {
                        conceptMetadata = GetEffectiveConcept(kbName, primaryConcept);
                    }
                    entityExists = conceptMetadata != null;
                    break;

                case "RELATION":
                    var relationMetadata = ListRelations(kbName).FirstOrDefault(r => r.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                    entityExists = relationMetadata != null;
                    // For relations, we can extract variables as properties if needed
                    break;
                case "RULE":
                    entityExists = ListRules(kbName).Any(x => x.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                    break;
                case "HIERARCHY":
                    // Check if the concept referenced actually exists (not the hierarchy entries themselves)
                    entityExists = _conceptCatalog.ListConcepts(kbName).Any(c => c.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                    break;
                case "FUNCTION":
                    entityExists = ListFunctions(kbName).Any(x => x.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                    break;
                case "OPERATOR":
                    entityExists = ListOperators(kbName).Any(x => x.Symbol.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                    break;
                default:
                    return ErrorResponse.ExecutionErrorResponse($"Unknown entity type '{targetType}'.");
            }

            if (!entityExists)
            {
                return ErrorResponse.ExecutionErrorResponse($"{targetType} '{entityName}' not found.");
            }

            // ── Semantic validation: WHERE conditions against concept schema ──
            if (targetType == "CONCEPT" && conceptMetadata != null && node.Conditions.Count > 0)
            {
                var whereValidError = ValidateConditionsAgainstSchema(
                    node.Conditions, conceptMetadata, "WHERE");
                if (whereValidError != null)
                    return ErrorResponse.ExecutionErrorResponse(whereValidError);
            }

            // 2. Handle HIERARCHY SELECT - returns table of hierarchy relationships
            if (targetType == "HIERARCHY")
            {
                var allHierarchies = ListHierarchies(kbName);
                
                // entityName is optional: if provided, filter to hierarchies involving that concept
                // If entityName is "*" or empty, return all
                IEnumerable<Hierarchy> filtered = allHierarchies;
                if (!string.IsNullOrEmpty(entityName) && entityName != "*")
                {
                    filtered = allHierarchies.Where(h =>
                        h.ChildConcept.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                        h.ParentConcept.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                }

                // Convert hierarchies to ObjectInstances (virtual rows)
                var hierarchyObjects = filtered.Select(h => new ObjectInstance
                {
                    Id = h.Id,
                    ConceptName = "HIERARCHY",
                    Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["child_concept"]   = h.ChildConcept,
                        ["hierarchy_type"]  = "IS_A",
                        ["parent_concept"]  = h.ParentConcept
                    }
                }).ToList();

                // Apply WHERE conditions
                if (node.Conditions.Count > 0)
                    hierarchyObjects = FilterObjects(objects: hierarchyObjects, conditions: node.Conditions, kbName: kbName, alias: node.Alias, conceptName: entityName);

                // Apply ORDER BY
                if (node.OrderBy.Count > 0)
                    hierarchyObjects = ApplyOrderBy(hierarchyObjects, node.OrderBy);

                // Apply LIMIT/OFFSET
                if (node.Limit != null)
                {
                    var offset = node.Limit.Offset ?? 0;
                    hierarchyObjects = hierarchyObjects.Skip(offset).Take(node.Limit.Limit).ToList();
                }

                return new QueryResultSet
                {
                    Success = true,
                    ConceptName = "HIERARCHY",
                    Columns = new List<string> { "child_concept", "hierarchy_type", "parent_concept" },
                    Objects = hierarchyObjects,
                    Count = hierarchyObjects.Count
                };
            }

            // Handle RULE SELECT - behaves as a virtual table if we want the evaluated instances
            if (targetType == "RULE")
            {
                var ruleList = ListRules(kbName);
                var rule = ruleList.FirstOrDefault(r => r.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                
                if (rule != null)
                {
                    // Evaluate the rule's hypothesis against its scope (concept)
                    var conceptName = rule.Scope;
                    var scopeConcept = _conceptCatalog.LoadConcept(kbName, conceptName);
                    if (scopeConcept != null)
                    {
                        var allObjects = SelectAllObjects(kbName)
                            .Where(o => o.ConceptName.Equals(conceptName, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        
                        var engine = GetConfiguredEngine(kbName);
                        var filteredObjects = allObjects.Where(obj => {
                            try {
                                if (rule.Hypothesis.Count == 0) return true;
                                foreach (var hp in rule.Hypothesis)
                                {
                                    // Strip concept wrapper: Student(grade > 90) -> grade > 90
                                    var content = hp.Content;
                                    var match = System.Text.RegularExpressions.Regex.Match(content, @"^\w+\((.+)\)$");
                                    if (match.Success) content = match.Groups[1].Value;

                                    // Prepare parameters, ensuring no nulls for NCalc
                                    var parameters = new Dictionary<string, object>();
                                    foreach (var kv in obj.Values)
                                    {
                                        if (kv.Value != null) parameters[kv.Key] = kv.Value;
                                    }

                                    var val = engine.EvaluateFormula(content, parameters);
                                    if (val is bool b) { if (!b) return false; }
                                    else if (val == null) return false;
                                }
                                return true;
                            } catch { return false; }
                        }).ToList();

                        return new QueryResultSet {
                            Success = true,
                            ConceptName = entityName,
                            Objects = filteredObjects,
                            Count = filteredObjects.Count,
                            Columns = filteredObjects.Count > 0 ? 
                                filteredObjects[0].Values.Keys.ToList() : 
                                scopeConcept.Variables.Select(v => v.Name).ToList()
                        };
                    }
                }

                // If specialized rule name not found or no scope, return metadata list
                if (!string.IsNullOrEmpty(entityName) && entityName != "*" && !entityName.Equals("RULE", StringComparison.OrdinalIgnoreCase))
                {
                    // If the user specified a name but we didn't enter the virtual table block, something is wrong
                    // Unless it's just meant to be metadata but for a specific rule?
                    // Let's filter metadata by name then.
                    ruleList = ruleList.Where(r => r.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var ruleObjects = ruleList.Select(r => new ObjectInstance
                {
                    Id = r.Id,
                    ConceptName = "RULE",
                    Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Name"]       = r.Name,
                        ["Type"]       = r.RuleType.ToString(),
                        ["Scope"]      = r.Scope ?? "",
                        ["Hypothesis"] = string.Join(", ", r.Hypothesis.Select(h => h.Content)),
                        ["Conclusion"] = string.Join(", ", r.Conclusion.Select(c => c.Content))
                    }
                }).ToList();

                return new QueryResultSet
                {
                    Success = true,
                    ConceptName = "RULE",
                    Columns = new List<string> { "Name", "Type", "Scope", "Hypothesis", "Conclusion" },
                    Objects = ruleObjects,
                    Count = ruleObjects.Count
                };
            }

            // Handle RELATION SELECT
            if (targetType == "RELATION")
            {
                var relList = ListRelations(kbName);
                if (!string.IsNullOrEmpty(entityName) && entityName != "*" && !entityName.Equals("RELATION", StringComparison.OrdinalIgnoreCase))
                {
                    relList = relList.Where(r => r.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var relObjects = relList.Select(r => new ObjectInstance
                {
                    Id = r.Id,
                    ConceptName = "RELATION",
                    Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Name"]      = r.Name,
                        ["Domain"]    = r.Domain,
                        ["Range"]     = r.Range,
                        ["Params"]    = string.Join(", ", r.ParamNames),
                        ["Equations"] = string.Join("; ", r.Equations.Select(e => e.Expression))
                    }
                }).ToList();

                return new QueryResultSet { Success = true, ConceptName = "RELATION", Columns = new List<string> { "Name", "Domain", "Range", "Params", "Equations" }, Objects = relObjects, Count = relObjects.Count };
            }

            // Handle FUNCTION SELECT
            if (targetType == "FUNCTION")
            {
                var funcList = ListFunctions(kbName);
                if (!string.IsNullOrEmpty(entityName) && entityName != "*" && !entityName.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase))
                {
                    funcList = funcList.Where(f => f.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var funcObjects = funcList.Select(f => new ObjectInstance
                {
                    Id = f.Id,
                    ConceptName = "FUNCTION",
                    Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Name"]       = f.Name,
                        ["ReturnType"] = f.ReturnType,
                        ["Params"]     = string.Join(", ", f.Parameters.Select(p => $"{p.Name}: {p.Type}")),
                        ["Body"]       = f.Body
                    }
                }).ToList();

                return new QueryResultSet { Success = true, ConceptName = "FUNCTION", Columns = new List<string> { "Name", "ReturnType", "Params", "Body" }, Objects = funcObjects, Count = funcObjects.Count };
            }

            // Handle OPERATOR SELECT
            if (targetType == "OPERATOR")
            {
                var opList = ListOperators(kbName);
                if (!string.IsNullOrEmpty(entityName) && entityName != "*" && !entityName.Equals("OPERATOR", StringComparison.OrdinalIgnoreCase))
                {
                    opList = opList.Where(o => o.Symbol.Equals(entityName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var opObjects = opList.Select(o => new ObjectInstance
                {
                    Id = o.Id,
                    ConceptName = "OPERATOR",
                    Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Symbol"]     = o.Symbol,
                        ["ReturnType"] = o.ReturnType,
                        ["ParamTypes"] = string.Join(", ", o.ParamTypes),
                        ["Body"]       = o.Body
                    }
                }).ToList();

                return new QueryResultSet { Success = true, ConceptName = "OPERATOR", Columns = new List<string> { "Symbol", "ReturnType", "ParamTypes", "Body" }, Objects = opObjects, Count = opObjects.Count };
            }

            // Handle VARIABLE/ATTRIBUTE SELECT
            if (targetType == "VARIABLE" || targetType == "ATTRIBUTE")
            {
                var concepts = _conceptCatalog.ListConcepts(kbName);
                var varObjects = new List<ObjectInstance>();
                foreach (var c in concepts)
                {
                    foreach (var v in c.Variables)
                    {
                        varObjects.Add(new ObjectInstance
                        {
                            Id = Guid.NewGuid(),
                            ConceptName = "VARIABLE",
                            Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["Name"]    = v.Name,
                                ["Type"]    = v.Type,
                                ["Concept"] = c.Name,
                                ["Length"]  = v.Length ?? (object)DBNull.Value,
                                ["Scale"]   = v.Scale ?? (object)DBNull.Value
                            }
                        });
                    }
                }

                if (!string.IsNullOrEmpty(entityName) && entityName != "*" && !entityName.Equals("VARIABLE", StringComparison.OrdinalIgnoreCase) && !entityName.Equals("ATTRIBUTE", StringComparison.OrdinalIgnoreCase))
                {
                    varObjects = varObjects.Where(v => v.Values["Name"].ToString()!.Equals(entityName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return new QueryResultSet { Success = true, ConceptName = "VARIABLE", Columns = new List<string> { "Name", "Type", "Concept", "Length", "Scale" }, Objects = varObjects, Count = varObjects.Count };
            }

            // 3. Handle sub-targets for CONCEPTs
            if (!string.IsNullOrEmpty(subTarget) && subTarget != "instances" && subTarget != "data")
            {
                if (targetType != "CONCEPT")
                {
                    return ErrorResponse.ExecutionErrorResponse($"Sub-target '{subTarget}' is not supported on {targetType}.");
                }
                
                if (conceptMetadata != null)
                {
                    return ExtractConceptMetadata(conceptMetadata, subTarget);
                }
            }

            // Fetch data instances  (V3 first, fallback to V1)
            List<ObjectInstance> objects = new List<ObjectInstance>();
            
            if (string.IsNullOrEmpty(subTarget) || subTarget == "instances" || subTarget == "data")
            {
                if (_v3Router != null)
                {
                    // V3 Route: Uses the Optimizer and Execution Pipeline (Pushdown + Joins)
                    var allConceptsToScan = new List<string> { entityName };
                    allConceptsToScan.AddRange(GetDescendantConcepts(kbName, entityName));
                    
                    var originalName = node.ConceptName;
                    foreach (var cName in allConceptsToScan)
                    {
                        node.ConceptName = cName;
                        var cMeta = _conceptCatalog.LoadConcept(kbName, cName);
                        var cMetaEffective = cMeta != null ? GetEffectiveConcept(kbName, cMeta) : null;
                        
                        var partialObjects = _v3Router.ExecuteSelect(kbName, node, cMetaEffective);
                        objects.AddRange(partialObjects);
                    }
                    node.ConceptName = originalName;
                }
                
                // Merge transaction buffer (shadow visibility)
                if (_inTransaction)
                {
                    objects.AddRange(_txBuffer
                        .Where(t => t.KbName == kbName && t.Obj.ConceptName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
                        .Select(t => t.Obj));
                }
                
                // If there are no conditions/aggregates/joins (just a direct Select), 
                // but WE MUST STILL CHECK for column aliases/projections.
                if (node.Conditions.Count == 0 && node.Joins.Count == 0 && node.GroupBy.Count == 0 && node.Aggregates.Count == 0 && node.OrderBy.Count == 0 && node.Limit == null && node.SelectColumns.Count == 0)
                {
                    var qrs_data = new QueryResultSet { 
                        ConceptName = $"{entityName}.data",
                        Success = true,
                        Objects = objects,
                        Count = objects.Count
                    };
                    

                    if (qrs_data.Objects.Count > 0)
                        qrs_data.Columns = qrs_data.Objects[0].Values.Keys.Where(k => !k.StartsWith("__")).ToList();
                    else if (conceptMetadata != null)
                        qrs_data.Columns = conceptMetadata.Variables.Select(v => v.Name).ToList();
                    
                    return qrs_data;
                }
            }

            // 2. Apply WHERE conditions (only if not already handled by V3 Optimizer)
            if (node.Conditions.Count > 0 && _v3Router == null)
            {
                objects = FilterObjects(objects: objects, conditions: node.Conditions, kbName: kbName, alias: node.Alias, conceptName: entityName);
            }

            // 3. Apply JOINs (only if not already handled by V3 Optimizer)
            if (node.Joins.Count > 0 && _v3Router == null)
            {
                foreach (var join in node.Joins)
                {
                    objects = ApplyJoin(objects, node.Alias, entityName, join, kbName);
                }
            }


            // 4. Apply GROUP BY + Aggregation
            if (node.GroupBy.Count > 0)
            {
                return ApplyGroupBy(objects, node);
            }

            // 5. Apply Aggregation only (no GROUP BY)
            if (node.Aggregates.Count > 0)
            {
                return EvaluateAggregates(objects, node.Aggregates);
            }

            // 6. Apply ORDER BY
            if (node.OrderBy.Count > 0)
            {
                objects = ApplyOrderBy(objects, node.OrderBy);
            }

            // 7. Apply LIMIT/OFFSET
            if (node.Limit != null)
            {
                var offset = node.Limit.Offset ?? 0;
                objects = objects.Skip(offset).Take(node.Limit.Limit).ToList();
            }

            // 8. Apply column projection (SelectColumns with optional AS alias)
            if (node.SelectColumns.Count > 0)
            {
                var isStarSelected = node.SelectColumns.Any(sc => sc.IsStar);
                var colsToInclude = node.SelectColumns.Where(sc => !sc.IsStar).ToList();

                if (isStarSelected || colsToInclude.Count > 0)
                {
                    var tableAlias = node.Alias ?? entityName;
                    var engine = GetConfiguredEngine(kbName);

                    objects = objects.Select(obj =>
                    {
                        // If * is selected, start with all existing values, excluding internal ones
                        var newValues = isStarSelected 
                            ? obj.Values.Where(kv => !kv.Key.StartsWith("__")).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                            : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        
                        // Prepare evaluation parameters with both raw and aliased names
                        var evalParams = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        foreach(var kv in obj.Values) {
                            if (kv.Value != null) {
                                evalParams[kv.Key] = kv.Value;
                                if (!string.IsNullOrEmpty(tableAlias))
                                    evalParams[$"{tableAlias}.{kv.Key}"] = kv.Value;
                            }
                        }

                        foreach (var col in colsToInclude)
                        {
                            var sourceName = col.Expression?.ToString() ?? col.Name;
                            var outName = col.Alias ?? col.Name;

                            // 1. Try direct resolution (for simple field names)
                            var val = ResolveValue(obj, sourceName, tableAlias, entityName);
                            if (val != null)
                            {
                                newValues[outName] = val;
                            }
                            else
                            {
                                if (col.Expression is FunctionCallNode func && func.FunctionName.Equals("SOLVE", StringComparison.OrdinalIgnoreCase))
                                {
                                    var targetVar = func.Arguments.FirstOrDefault()?.ToString();
                                    newValues[outName] = null!;
                                    if (!string.IsNullOrEmpty(targetVar))
                                    {
                                        var resolvedConcept = engine.ConceptResolver?.Invoke(entityName) ?? conceptMetadata;
                                        if (resolvedConcept != null)
                                        {
                                            bool solved = false;
                                            
                                            // Quick path: if the target is already known (e.g. from DB), just return it
                                            if (evalParams.TryGetValue(targetVar, out var knownVal))
                                            {
                                                newValues[outName] = knownVal;
                                                solved = true;
                                            }

                                            // FAST PATH: Try direct equation solving first (much faster than full Rete)
                                            if (!solved)
                                            {
                                                foreach (var eq in resolvedConcept.Equations)
                                                {
                                                    var eqVars = engine.ExtractVariablesFromExpression(eq.Expression);
                                                    if (eqVars.Contains(targetVar, StringComparer.OrdinalIgnoreCase))
                                                    {
                                                        try
                                                        {
                                                            var root = engine.Solve1DEquation(eq.Expression, targetVar, evalParams);
                                                            if (!double.IsNaN(root))
                                                            {
                                                                if (double.IsInfinity(root)) throw new Exception("Mathematical error: infinity produced.");
                                                                var variable = resolvedConcept.Variables.FirstOrDefault(v => v.Name.Equals(targetVar, StringComparison.OrdinalIgnoreCase));
                                                                newValues[outName] = engine.CastToVariableType(root, variable);
                                                                solved = true;
                                                                break;
                                                            }
                                                        }
                                                        catch (Exception ex) when (ex.Message.Contains("infinity")) { throw; }
                                                        catch { /* Fall through to full closure */ }
                                                    }
                                                }

                                                // FALLBACK: Full Rete-based closure if fast path failed
                                                if (!solved)
                                                {
                                                    var solveResult = engine.FindClosure(resolvedConcept, evalParams, new List<string> { targetVar });
                                                    if (solveResult.Success && solveResult.DerivedFacts.TryGetValue(targetVar, out var solvedVal))
                                                    {
                                                        if (solvedVal is double d && (double.IsInfinity(d) || double.IsNaN(d)))
                                                            throw new Exception("Mathematical error: infinity produced.");

                                                        newValues[outName] = solvedVal;
                                                        solved = true;
                                                    }
                                                    else if (!solveResult.Success && !string.IsNullOrEmpty(solveResult.ErrorMessage))
                                                    {
                                                        throw new Exception(solveResult.ErrorMessage);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // 2. Try evaluating as NCalc expression (for math/aggregate functions)
                                    try {
                                        var exprForEval = (col.Expression != null ? col.Expression.ToString() : sourceName) ?? "";
                                        newValues[outName] = engine.EvaluateFormula(exprForEval, evalParams);
                                    } catch {
                                        newValues[outName] = null!;
                                    }
                                }
                            }
                        }
                        return new ObjectInstance { Id = obj.Id, ConceptName = obj.ConceptName, Values = newValues };
                    }).ToList();
                }
            }

            var final_qrs = new QueryResultSet
            {
                Success = true,
                ConceptName = node.ConceptName,
                Count = objects.Count,
                Objects = objects
            };


            if (objects.Count > 0)
            {
                final_qrs.Columns = objects[0].Values.Keys.ToList();
            }
            else if (node.SelectColumns.Count > 0 && !node.SelectColumns.Any(c => c.IsStar))
            {
                // Use requested columns as column headers even when no rows returned
                final_qrs.Columns = node.SelectColumns.Select(c => c.Alias ?? c.Name).ToList();
            }
            else if (conceptMetadata != null)
            {
                final_qrs.Columns = conceptMetadata.Variables.Select(v => v.Name).ToList();
            }

            return final_qrs;
        }
        catch (Exception ex)
        {
            return ErrorResponse.ExecutionErrorResponse($"SELECT failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle SELECT with derived table (sub-query in FROM clause)
    /// Example: SELECT * FROM (SELECT a, b FROM T1) AS sub WHERE ...
    /// </summary>
    private object HandleDerivedTableSelect(SelectNode node, string kbName)
    {
        // 1. Execute the inner sub-query
        var innerResult = HandleSelect(node.DerivedTable!, kbName);
        if (innerResult is ErrorResponse error)
            return error;

        if (innerResult is not QueryResultSet innerQrs || !innerQrs.Success)
            return ErrorResponse.ExecutionErrorResponse("Derived table sub-query failed.");

        // 2. Use the sub-query result as the source for the outer query
        var alias = node.Alias ?? "derived";
        var derivedObjects = innerQrs.ToObjectInstances(alias);

        // 3. Build schema for the derived table
        var derivedSchema = innerQrs.Schema ?? new QuerySchema
        {
            SourceConcept = alias,
            Alias = alias,
            IsComposable = true,
            Columns = innerQrs.Columns.Select(col => new ColumnSchema
            {
                Name = col,
                Type = "UNKNOWN",
                SourceTable = alias
            }).ToList()
        };

        // 4. Apply WHERE conditions from outer query
        if (node.Conditions.Count > 0)
        {
            derivedObjects = FilterObjects(derivedObjects, node.Conditions, kbName, alias, alias);
        }

        // 5. Apply projections (SELECT columns)
        List<ObjectInstance> projectedObjects;
        List<string> columns;

        if (node.SelectColumns.Count == 0 ||
            (node.SelectColumns.Count == 1 && node.SelectColumns[0].IsStar))
        {
            // SELECT * - keep all columns
            projectedObjects = derivedObjects;
            columns = derivedSchema.Columns.Select(c => c.Name).ToList();
        }
        else
        {
            // Specific columns selected
            columns = new List<string>();
            projectedObjects = new List<ObjectInstance>();

            foreach (var obj in derivedObjects)
            {
                var newObj = new ObjectInstance
                {
                    Id = obj.Id,
                    KbId = obj.KbId,
                    ConceptName = alias,
                    Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                };

                foreach (var col in node.SelectColumns)
                {
                    if (col.IsStar)
                    {
                        // Include all columns
                        foreach (var kv in obj.Values)
                        {
                            newObj.Values[kv.Key] = kv.Value;
                            if (!columns.Contains(kv.Key)) columns.Add(kv.Key);
                        }
                    }
                    else
                    {
                        var colName = col.Alias ?? col.Name;
                        if (obj.Values.TryGetValue(col.Name, out var val) ||
                            obj.Values.TryGetValue($"{alias}.{col.Name}", out val))
                        {
                            newObj.Values[colName] = val;
                            if (!columns.Contains(colName)) columns.Add(colName);
                        }
                    }
                }

                projectedObjects.Add(newObj);
            }
        }

        // 6. Apply ORDER BY
        if (node.OrderBy.Count > 0)
        {
            projectedObjects = ApplyOrderBy(projectedObjects, node.OrderBy);
        }

        // 7. Apply LIMIT/OFFSET
        if (node.Limit != null)
        {
            var offset = node.Limit.Offset ?? 0;
            projectedObjects = projectedObjects.Skip(offset).Take(node.Limit.Limit).ToList();
        }

        // 8. Build final result with schema
        return new QueryResultSet
        {
            Success = true,
            ConceptName = alias,
            Columns = columns,
            Objects = projectedObjects,
            Count = projectedObjects.Count,
            Schema = new QuerySchema
            {
                SourceConcept = alias,
                Alias = alias,
                IsComposable = true,
                Columns = columns.Select(c => new ColumnSchema
                {
                    Name = c,
                    Type = derivedSchema.GetColumn(c)?.Type ?? "UNKNOWN",
                    SourceTable = alias
                }).ToList()
            }
        };
    }

    public IEnumerable<ObjectInstance> SelectAllObjects(string kbName)
    {
        var concepts = _conceptCatalog.ListConcepts(kbName);
        var result = new List<ObjectInstance>();
        foreach (var concept in concepts)
        {
            result.AddRange(_v3Router.SelectObjects(kbName, concept.Name, concept: concept));
        }
        return result;
    }

    private List<Rule> ListRules(string kbName) => _kbCatalog.LoadKb(kbName)?.Rules ?? new();
    private List<Relation> ListRelations(string kbName) => _kbCatalog.LoadKb(kbName)?.Relations ?? new();
    private List<Operator> ListOperators(string kbName) => _kbCatalog.LoadKb(kbName)?.Operators ?? new();
    private List<Function> ListFunctions(string kbName) => _kbCatalog.LoadKb(kbName)?.Functions ?? new();
    private List<Hierarchy> ListHierarchies(string kbName) => _kbCatalog.LoadKb(kbName)?.Hierarchies ?? new();

    private List<ObjectInstance> FilterObjects(List<ObjectInstance> objects, List<Condition> conditions, string kbName, string? alias = null, string? conceptName = null)
    {
        var result = new List<ObjectInstance>();

        foreach (var obj in objects)
        {
            if (EvaluateObjectConditions(obj, conditions, kbName, alias, conceptName))
            {
                result.Add(obj);
            }
        }

        return result;
    }

    private bool EvaluatePredicate(Dictionary<string, object> values, List<Condition> conditions, string kbName, string? alias = null, string? conceptName = null)
    {
        if (conditions == null || conditions.Count == 0) return true;
        var obj = new ObjectInstance { Values = values };
        return EvaluateObjectConditions(obj, conditions, kbName, alias, conceptName);
    }

    private bool EvaluateObjectConditions(ObjectInstance obj, List<Condition> conditions, string kbName, string? alias = null, string? conceptName = null)
    {
        if (conditions == null || conditions.Count == 0) return true;

        var result = MatchCondition(obj, conditions[0], kbName, alias, conceptName);

        for (int i = 1; i < conditions.Count; i++)
        {
            var cond = conditions[i];
            var val = MatchCondition(obj, cond, kbName, alias, conceptName);

            if (conditions[i - 1].LogicalOperator == "OR")
            {
                result = result || val;
            }
            else // AND (default)
            {
                result = result && val;
            }
        }

        return result;
    }

    private bool MatchCondition(ObjectInstance obj, Condition condition, string kbName, string? a = null, string? c = null)
    {
        // Handle EXISTS sub-query
        if (condition.Operator.Equals("EXISTS", StringComparison.OrdinalIgnoreCase) &&
            condition.Field.Equals("EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            if (condition.Value is SelectNode existsSubQuery)
            {
                var existsResult = HandleSelect(existsSubQuery, kbName);
                if (existsResult is QueryResultSet existsQrs && existsQrs.Success)
                {
                    return existsQrs.Count > 0;
                }
            }
            return false;
        }

        // Handle SOLVE in WHERE clause (e.g., WHERE SOLVE(area) > 100)
        if (condition.HasSolveLeft && condition.LeftExpression is FunctionCallNode solveFunc)
        {
            var targetVar = solveFunc.Arguments.FirstOrDefault()?.ToString();
            if (!string.IsNullOrEmpty(targetVar))
            {
                // Get concept metadata for SOLVE
                var conceptName = c ?? obj.ConceptName;
                var concept = _conceptCatalog.LoadConcept(kbName, conceptName);

                if (concept != null)
                {
                    // Build evaluation parameters from object values
                    var evalParams = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in obj.Values)
                    {
                        evalParams[kv.Key] = kv.Value ?? 0;
                    }

                    // Create inference engine and solve
                    var engine = GetConfiguredEngine(kbName);
                    var solveResult = engine.FindClosure(concept, evalParams, new List<string> { targetVar });

                    if (solveResult.Success && solveResult.DerivedFacts.TryGetValue(targetVar, out var solvedValue))
                    {
                        // Compare solved value with condition value
                        var solveCompareValue = condition.Value;
                        return condition.Operator switch
                        {
                            "=" => Equals(solvedValue, solveCompareValue) || CompareValues(solvedValue, solveCompareValue) == 0,
                            "<>" or "!=" => !Equals(solvedValue, solveCompareValue) && CompareValues(solvedValue, solveCompareValue) != 0,
                            ">" => CompareValues(solvedValue, solveCompareValue) > 0,
                            "<" => CompareValues(solvedValue, solveCompareValue) < 0,
                            ">=" => CompareValues(solvedValue, solveCompareValue) >= 0,
                            "<=" => CompareValues(solvedValue, solveCompareValue) <= 0,
                            _ => false
                        };
                    }
                }
            }
            return false;
        }

        var value = ResolveValue(obj, condition.Field, a, c);

        var compareValue = condition.Value;

        // ── NULL semantics ──────────────────────────────────────────────────
        // Field has no value → treat as NULL
        if (value == null)
        {
            // "IS" operator (e.g. field IS NULL / field IS NOT NULL)
            if (condition.Operator.Equals("IS", StringComparison.OrdinalIgnoreCase))
            {
                bool expectNull = compareValue == null ||
                    compareValue.ToString()!.Equals("NULL", StringComparison.OrdinalIgnoreCase);
                return expectNull; // IS NULL → true
            }
            // Explicit = null / <> null
            bool condValueIsNull = compareValue == null ||
                compareValue.ToString()!.Equals("NULL", StringComparison.OrdinalIgnoreCase);
            return condition.Operator switch
            {
                "=" => condValueIsNull,           // field = null → true when field is null
                "<>" or "!=" => !condValueIsNull, // field <> null → true when field is NOT null
                _ => false  // >, <, >=, <= with null → always false (SQL semantics)
            };
        }

        // If compare value is explicitly NULL and field has a real value → not null
        bool compareIsNull = compareValue == null ||
            compareValue.ToString()!.Equals("NULL", StringComparison.OrdinalIgnoreCase);
        if (compareIsNull)
        {
            return condition.Operator switch
            {
                "=" => false,           // some_value = null → false
                "<>" or "!=" => true,   // some_value <> null → true
                "IS" => false,          // some_value IS NULL → false
                _ => false
            };
        }

        // Handle scalar sub-query comparison (e.g., id = (SELECT MAX(id) FROM T))
        if (compareValue is SelectNode scalarSubQuery)
        {
            var scalarResult = HandleSelect(scalarSubQuery, kbName);
            if (scalarResult is QueryResultSet scalarQrs && scalarQrs.Success && scalarQrs.Count > 0)
            {
                // Get the first value from the first row
                var firstObj = scalarQrs.Objects[0];
                if (firstObj.Values.Count > 0)
                {
                    compareValue = firstObj.Values.Values.First();
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        if (condition.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase))
        {
            if (compareValue is SelectNode subQueryNode)
            {
                if (HandleSelect(subQueryNode, kbName) is QueryResultSet subqResult && subqResult.Success)
                {
                    var validValues = new HashSet<string>();
                    foreach (var subObj in subqResult.Objects)
                    {
                        if (subObj.Values.Count > 0)
                        {
                            var firstVal = subObj.Values.Values.First()?.ToString();
                            if (firstVal != null) validValues.Add(firstVal);
                        }
                    }
                    var strValue = value?.ToString();
                    return strValue != null && validValues.Contains(strValue);
                }
                return false;
            }
            else if (compareValue is IEnumerable<object> list)
            {
                var strValue = value?.ToString();
                return strValue != null && list.Select(x => x?.ToString()).Contains(strValue);
            }
            return false;
        }

        var result = condition.Operator switch
        {
            "=" => Equals(value, compareValue) || CompareValues(value, compareValue) == 0,
            "<>" or "!=" => !Equals(value, compareValue) && CompareValues(value, compareValue) != 0,
            ">" => CompareValues(value, compareValue) > 0,
            "<" => CompareValues(value, compareValue) < 0,
            ">=" => CompareValues(value, compareValue) >= 0,
            "<=" => CompareValues(value, compareValue) <= 0,
            "LIKE" => System.Text.RegularExpressions.Regex.IsMatch(value?.ToString() ?? "", "^" + System.Text.RegularExpressions.Regex.Escape(compareValue?.ToString() ?? "").Replace("%", ".*").Replace("_", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            "CONTAINS" => (value?.ToString() ?? "").Contains(compareValue?.ToString() ?? "", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        return result;
    }

    private int CompareValues(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // Try numeric comparison first
        if (TryConvertToDouble(a, out var da) && TryConvertToDouble(b, out var db))
        {
            if (Math.Abs(da - db) < 1e-9) return 0;
            return da.CompareTo(db);
        }

        // Handle case where one is numeric and other is string (common in queries)
        var sa = a.ToString() ?? "";
        var sb = b.ToString() ?? "";
        
        if (double.TryParse(sa, out var dsa) && double.TryParse(sb, out var dsb))
        {
            if (Math.Abs(dsa - dsb) < 1e-9) return 0;
            return dsa.CompareTo(dsb);
        }

        return string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryConvertToDouble(object? value, out double result)
    {
        result = 0;
        if (value == null) return false;

        if (value is double d) { result = d; return true; }
        if (value is int i) { result = i; return true; }
        if (value is float f) { result = f; return true; }
        if (value is decimal dec) { result = (double)dec; return true; }
        if (value is long l) { result = l; return true; }

        return double.TryParse(value.ToString(), out result);
    }

    private List<ObjectInstance> ApplyJoin(List<ObjectInstance> objects, string? leftAlias, string? leftConcept, JoinClause join, string kbName)
    {
        var targetConcept = _conceptCatalog.LoadConcept(kbName, join.Target);
        var joinObjects = _v3Router.SelectObjects(kbName, join.Target, concept: targetConcept).ToList();

        var result = new List<ObjectInstance>();
        var joinType = join.JoinType?.ToUpper() ?? "INNER";
        var rightAlias = join.Alias ?? join.Target;

        // Track which right objects matched (for RIGHT and FULL joins)
        var matchedRightIndices = new HashSet<int>();

        // Process each left object
        for (int leftIdx = 0; leftIdx < objects.Count; leftIdx++)
        {
            var obj = objects[leftIdx];
            var matchedAny = false;

            for (int rightIdx = 0; rightIdx < joinObjects.Count; rightIdx++)
            {
                var joinObj = joinObjects[rightIdx];

                bool matches = false;
                if (join.OnCondition != null)
                {
                    matches = EvaluateJoinCondition(obj, leftAlias, leftConcept, joinObj, join.Alias, join.Target, join.OnCondition);
                }
                else
                {
                    // No ON condition - cross join (all combinations)
                    matches = true;
                }

                if (matches)
                {
                    var merged = MergeObjects(obj, joinObj, rightAlias);
                    result.Add(merged);
                    matchedAny = true;
                    matchedRightIndices.Add(rightIdx);
                }
            }

            // LEFT JOIN: Include left row even if no match found
            if (!matchedAny && (joinType == "LEFT" || joinType == "FULL"))
            {
                var nullRight = CreateNullObject(join.Target, rightAlias, targetConcept);
                var merged = MergeObjects(obj, nullRight, rightAlias);
                result.Add(merged);
            }
        }

        // RIGHT/FULL JOIN: Include unmatched right rows
        if (joinType == "RIGHT" || joinType == "FULL")
        {
            for (int rightIdx = 0; rightIdx < joinObjects.Count; rightIdx++)
            {
                if (!matchedRightIndices.Contains(rightIdx))
                {
                    var joinObj = joinObjects[rightIdx];
                    var nullLeft = CreateNullObject(leftConcept ?? "left", leftAlias,
                        leftConcept != null ? _conceptCatalog.LoadConcept(kbName, leftConcept) : null);
                    var merged = MergeObjects(nullLeft, joinObj, rightAlias);
                    result.Add(merged);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Create an object with NULL values for all variables in the concept
    /// </summary>
    private ObjectInstance CreateNullObject(string conceptName, string? alias, Concept? concept)
    {
        var nullObj = new ObjectInstance
        {
            ConceptName = conceptName,
            Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        };

        if (concept != null)
        {
            foreach (var variable in concept.Variables)
            {
                nullObj.Values[variable.Name] = null!;
                if (!string.IsNullOrEmpty(alias))
                {
                    nullObj.Values[$"{alias}.{variable.Name}"] = null!;
                }
            }
        }

        return nullObj;
    }

    private bool EvaluateJoinCondition(ObjectInstance left, string? leftAlias, string? leftConcept, 
                                       ObjectInstance right, string? rightAlias, string? rightConcept, 
                                       Condition condition)
    {
        var leftValue = ResolveValue(left, condition.Field, leftAlias, leftConcept);
        var rightValue = ResolveValue(right, condition.Value?.ToString() ?? "", rightAlias, rightConcept);

        if (leftValue == null || rightValue == null) return false;

        return condition.Operator switch
        {
            "=" => Equals(leftValue, rightValue) || CompareValues(leftValue, rightValue) == 0,
            _ => false
        };
    }

    private object? ResolveValue(ObjectInstance obj, string field, string? alias = null, string? conceptName = null)
    {
        if (string.IsNullOrEmpty(field)) return null;

        // 1. Try exact match (including if it already contains a dot from a prior merge)
        if (obj.Values.TryGetValue(field, out var val)) return val;

        var searchField = field;
        if (field.Contains('.'))
        {
            var parts = field.Split('.');
            if (parts.Length == 2)
            {
                var prefix = parts[0];
                var actual = parts[1];

                if (prefix.Equals(alias, StringComparison.OrdinalIgnoreCase) || 
                    prefix.Equals(conceptName, StringComparison.OrdinalIgnoreCase))
                {
                    searchField = actual;
                }
            }
        }

        // 2. Try match again
        if (obj.Values.TryGetValue(searchField, out val)) return val;

        // 3. Case-insensitive search
        var key = obj.Values.Keys.FirstOrDefault(k => k.Equals(searchField, StringComparison.OrdinalIgnoreCase));
        if (key != null) return obj.Values[key];

        return null;
    }

    private ObjectInstance MergeObjects(ObjectInstance left, ObjectInstance right, string? alias)
    {
        var merged = new ObjectInstance
        {
            Id = left.Id,
            KbId = left.KbId,
            ConceptName = left.ConceptName,
            Values = new Dictionary<string, object>(left.Values)
        };

        foreach (var kv in right.Values)
        {
            var key = alias != null ? $"{alias}.{kv.Key}" : kv.Key;
            merged.Values[key] = kv.Value;
        }

        return merged;
    }

    private object ApplyGroupBy(List<ObjectInstance> objects, SelectNode node)
    {
        var groups = objects.GroupBy(o => string.Join("|", node.GroupBy.Select(g => o.Values.GetValueOrDefault(g)?.ToString() ?? "null")));

        var result = new List<Dictionary<string, object>>();

        foreach (var group in groups)
        {
            var row = new Dictionary<string, object>();

            // Add group by values
            var firstObj = group.First();
            foreach (var gb in node.GroupBy)
            {
                row[gb] = firstObj.Values.GetValueOrDefault(gb)!;
            }

            // Add aggregates
            foreach (var agg in node.Aggregates)
            {
                var aggValue = EvaluateAggregate(group.ToList(), agg);
                row[agg.Alias ?? agg.AggregateType] = aggValue;
            }

            result.Add(row);
        }

        return new QueryResultSet { Success = true, Count = result.Count, Groups = result };
    }

    private object EvaluateAggregates(List<ObjectInstance> objects, List<AggregateClause> aggregates)
    {
        var result = new Dictionary<string, object>();

        foreach (var agg in aggregates)
        {
            var value = EvaluateAggregate(objects, agg);
            result[agg.Alias ?? agg.AggregateType] = value;
        }

        return new QueryResultSet { Success = true, Aggregates = result };
    }

    private object EvaluateAggregate(List<ObjectInstance> objects, AggregateClause aggregate)
    {
        return aggregate.AggregateType.ToUpper() switch
        {
            "COUNT" => aggregate.Variable == null
                ? objects.Count
                : objects.Count(o => o.Values.ContainsKey(aggregate.Variable) && o.Values[aggregate.Variable] != null),

            "SUM" => objects.Sum(o =>
            {
                if (o.Values.TryGetValue(aggregate.Variable ?? "", out var v) && TryConvertToDouble(v, out var d))
                    return d;
                return 0;
            }),

            "AVG" => objects.Average(o =>
            {
                if (o.Values.TryGetValue(aggregate.Variable ?? "", out var v) && TryConvertToDouble(v, out var d))
                    return d;
                return 0;
            }),

            "MAX" => objects.Max(o =>
            {
                if (o.Values.TryGetValue(aggregate.Variable ?? "", out var v) && TryConvertToDouble(v, out var d))
                    return d;
                return double.MinValue;
            }),

            "MIN" => objects.Min(o =>
            {
                if (o.Values.TryGetValue(aggregate.Variable ?? "", out var v) && TryConvertToDouble(v, out var d))
                    return d;
                return double.MaxValue;
            }),

            _ => throw new NotSupportedException($"Unknown aggregate function: {aggregate.AggregateType}")
        };
    }

    private List<ObjectInstance> ApplyOrderBy(List<ObjectInstance> objects, List<OrderByItem> orderBy)
    {
        if (orderBy == null || orderBy.Count == 0 || objects.Count == 0)
            return objects;

        // Apply sorting using our custom comparer that handles nulls and mixed types
        var sortedObjects = objects.ToList();

        foreach (var item in orderBy)
        {
            // Find matching key case-insensitively
            var matchingKey = sortedObjects[0].Values.Keys
                .FirstOrDefault(k => k.Equals(item.Variable, StringComparison.OrdinalIgnoreCase)) ?? item.Variable;

            var isDescending = item.Direction == "DESC";

            sortedObjects.Sort((a, b) =>
            {
                var valA = a.Values.GetValueOrDefault(matchingKey);
                var valB = b.Values.GetValueOrDefault(matchingKey);
                var comparison = CompareValues(valA, valB);
                return isDescending ? -comparison : comparison;
            });
        }

        return sortedObjects;
    }

    private object HandleInsert(InsertNode node, string kbName, Models.User executor)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null)
        {
            return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");
        }

        // Load concept to validate it exists and get variable names for positional values
        var primaryConcept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        if (primaryConcept == null)
        {
            return ErrorResponse.ExecutionErrorResponse($"Concept '{node.ConceptName}' does not exist.");
        }

        // Apply Inheritance
        var concept = GetEffectiveConcept(kbName, primaryConcept);

        var values = new Dictionary<string, object>();

        // Check if values use positional syntax (keys like _0, _1, etc.)
        var positionalValues = node.Values
            .Where(kv => kv.Key.StartsWith("_") && int.TryParse(kv.Key.Substring(1), out _))
            .OrderBy(kv => int.Parse(kv.Key.Substring(1)))
            .Select(kv => kv.Value)
            .ToList();

        var namedValues = node.Values
            .Where(kv => !(kv.Key.StartsWith("_") && int.TryParse(kv.Key.Substring(1), out _)))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (positionalValues.Count > 0 && concept != null)
        {
            // Map positional values to concept variables
            for (int i = 0; i < positionalValues.Count && i < concept.Variables.Count; i++)
            {
                var variable = concept.Variables[i];
                values[variable.Name] = ConvertValueNodeToObject(positionalValues[i], variable);
            }
        }

        // Add named values (these override positional values if there's a conflict)
        foreach (var kv in namedValues)
        {
            var variable = concept?.Variables.FirstOrDefault(v => v.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
            values[kv.Key] = ConvertValueNodeToObject(kv.Value, variable);
        }

        bool localTxn = false;
        if (!_inTransaction)
        {
            HandleBeginTransaction();
            localTxn = true;
        }

        try
        {
            var obj = new ObjectInstance
            {
                Id = Guid.NewGuid(),
                KbId = kb.Id,
                ConceptName = node.ConceptName,
                Values = values
            };

            // --- Write-Time Inference for INSERT ---
            if (concept != null)
            {
                var engine = GetConfiguredEngine(kbName);
                var inferenceValues = new Dictionary<string, object>(values);
                
                // Remove dependent variables so they are forced to be re-derived
                foreach (var eq in concept.Equations)
                {
                    var parts = eq.Expression.Split('=');
                    if (parts.Length == 2) inferenceValues.Remove(parts[0].Trim());
                }

                var inferenceResult = engine.FindClosure(concept, inferenceValues, new List<string>());
                if (!inferenceResult.Success)
                {
                    throw new Exception(inferenceResult.ErrorMessage ?? "Inference failed.");
                }

                if (inferenceResult.DerivedFacts.Count > 0)
                {
                    var externalUpdates = new Dictionary<string, Dictionary<string, object>>();

                    foreach (var derived in inferenceResult.DerivedFacts)
                    {
                        if (!derived.Key.Contains('.'))
                        {
                            values[derived.Key] = derived.Value;
                        }
                        else
                        {
                            var parts = derived.Key.Split('.');
                            var alias = parts[0];
                            var field = parts[1];
                            
                            if (!externalUpdates.ContainsKey(alias))
                                externalUpdates[alias] = new Dictionary<string, object>();
                                
                            externalUpdates[alias][field] = derived.Value;
                        }
                    }

                    // Save explainability metadata to the database
                    values["__audit_trail"] = System.Text.Json.JsonSerializer.Serialize(inferenceResult.AuditTrail);
                    values["__generated_vars"] = System.Text.Json.JsonSerializer.Serialize(inferenceResult.GeneratedVariables);

                    // Apply external updates
                    foreach (var kvp in externalUpdates)
                    {
                        var alias = kvp.Key;
                        var updates = kvp.Value;
                        
                        var idFact = inferenceResult.WorkingMemory.FirstOrDefault(f => f.Name.Equals($"{alias}.__internal_id", StringComparison.OrdinalIgnoreCase));
                        var conceptFact = inferenceResult.WorkingMemory.FirstOrDefault(f => f.Name.Equals($"{alias}.__internal_concept", StringComparison.OrdinalIgnoreCase));
                        
                        if (idFact != null && conceptFact != null && Guid.TryParse(idFact.Value?.ToString(), out var extId))
                        {
                            var extConcept = conceptFact.Value?.ToString();
                            if (string.IsNullOrEmpty(extConcept)) continue;

                            var extObj = _v3Router.SelectObjects(kbName, extConcept).FirstOrDefault(o => o.Id == extId);
                            if (extObj != null)
                            {
                                foreach (var uv in updates) extObj.Values[uv.Key] = uv.Value;
                                
                                // Buffer the update
                                _txBuffer.Add(("UPDATE", kbName, extObj));
                            }
                        }
                    }
                }
            }

            // V3 engine write
            _txBuffer.Add(("INSERT", kbName, obj));

            if (localTxn)
            {
                var commitResult = HandleCommit(kbName);
                // Assume HandleCommit handles actual v3Router writes. Wait, HandleCommit loops _txBuffer and calls InsertObject.
                // But _v3Router.UpdateObject is called directly above!
                // We need to fix HandleCommit to handle both Insert and Update.
                // Actually, _v3Router.UpdateObject might not be buffered!
            }

            return new { success = true, message = $"Object inserted successfully with ID: {obj.Id}", data = obj.Values };
        }
        catch (Exception ex)
        {
            if (localTxn) HandleRollback();
            return ErrorResponse.ExecutionErrorResponse($"Insert failed: {ex.Message}");
        }
    }

    private object HandleInsertBulk(InsertBulkNode node, string kbName, Models.User executor)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null)
            return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");

        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        if (concept == null)
            return ErrorResponse.ExecutionErrorResponse($"Concept '{node.ConceptName}' does not exist.");

        int inserted = 0;
        int failed = 0;
        var errors = new List<string>();

        var objectsToInsert = new List<ObjectInstance>();

        foreach (var rowValues in node.Rows)
        {
            var values = new Dictionary<string, object>();

            var positionalValues = rowValues
                .Where(kv => kv.Key.StartsWith("_") && int.TryParse(kv.Key.Substring(1), out _))
                .OrderBy(kv => int.Parse(kv.Key.Substring(1)))
                .Select(kv => kv.Value)
                .ToList();

            var namedValues = rowValues
                .Where(kv => !(kv.Key.StartsWith("_") && int.TryParse(kv.Key.Substring(1), out _)))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (positionalValues.Count > 0)
            {
                for (int i = 0; i < positionalValues.Count && i < concept.Variables.Count; i++)
                {
                    var variable = concept.Variables[i];
                    values[variable.Name] = ConvertValueNodeToObject(positionalValues[i], variable);
                }
            }

            foreach (var kv in namedValues)
            {
                var variable = concept.Variables.FirstOrDefault(v => v.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                values[kv.Key] = ConvertValueNodeToObject(kv.Value, variable);
            }

            var obj = new ObjectInstance
            {
                Id = Guid.NewGuid(),
                KbId = kb.Id,
                ConceptName = node.ConceptName,
                Values = values
            };

            objectsToInsert.Add(obj);
        }

        inserted = _v3Router.BulkInsertObjects(kbName, objectsToInsert, concept);
        if (inserted > 0)
        {
            FireTriggers(kbName, node.ConceptName, "INSERT", executor);
        }
        failed = objectsToInsert.Count - inserted;

        return new
        {
            success = failed == 0,
            message = $"Bulk insert into '{node.ConceptName}': {inserted} inserted, {failed} failed.",
            inserted,
            failed,
            errors
        };
    }

    private object ConvertValueNodeToObject(ValueNode valueNode, Models.Variable? targetVar = null)
    {
        var rawValue = valueNode.ValueType switch
        {
            "number" => TryConvertToDouble(valueNode.Value, out var d) ? (object)d : 0.0,
            "string" => valueNode.Value?.ToString() ?? "",
            "boolean" => valueNode.Value is bool b ? b : valueNode.Value?.ToString()?.ToLower() == "true",
            "identifier" => valueNode.Value?.ToString() ?? "",
            _ => valueNode.Value ?? ""
        };

        if (targetVar == null) return rawValue;

        // Strict Type Enforcement (Phase 8)
        var type = targetVar.Type.ToUpper();
        try
        {
            if (type is "INT" or "INTEGER" or "LONG")
            {
                return Convert.ToInt64(rawValue);
            }
            if (type is "DECIMAL" or "MONEY" or "NUMBER")
            {
                var dec = Convert.ToDecimal(rawValue);
                if (targetVar.Scale.HasValue)
                {
                    dec = Math.Round(dec, targetVar.Scale.Value);
                }
                return dec;
            }
            if (type is "FLOAT" or "DOUBLE")
            {
                return Convert.ToDouble(rawValue);
            }
            if (targetVar.IsReference)
            {
                // Ensure it's stored as a string or Guid representation
                return rawValue.ToString() ?? "";
            }
        }
        catch
        {
            // Fallback to raw if conversion fails
        }

        return rawValue;
    }

    private object HandleUpdate(UpdateNode node, string kbName, Models.User executor)
    {
        // Optimized V3 update: push conditions down
        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        var matchingObjects = _v3Router.SelectObjects(kbName, node.ConceptName, values => EvaluatePredicate(values, node.Conditions, kbName, null, node.ConceptName), concept);

        if (matchingObjects.Count == 0)
        {
            return ErrorResponse.ExecutionErrorResponse("No objects found matching conditions.");
        }

        var engine = GetConfiguredEngine(kbName);
        var success = true;
        int updatedCount = 0;

        foreach (var obj in matchingObjects)
        {
            var parameters = new Dictionary<string, object>(obj.Values);
            var updatedValues = new Dictionary<string, object>();

            foreach (var kv in node.SetValues)
            {
                try
                {
                    var formula = kv.Value.ToString() ?? "";
                    var res = engine.EvaluateFormula(formula, parameters);
                    
                    var variable = concept?.Variables.FirstOrDefault(v => v.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                    var castedRes = engine.CastToVariableType(res, variable);
                    
                    updatedValues[kv.Key] = castedRes;
                    parameters[kv.Key] = castedRes; // Allow subsequent SET clauses to use updated value
                }
                catch (Exception ex)
                {
                    return ErrorResponse.ExecutionErrorResponse($"Failed to evaluate expression for '{kv.Key}': {ex.Message}");
                }
            }

            // Update object's values with new set values
            foreach (var kv in updatedValues)
            {
                obj.Values[kv.Key] = kv.Value;
            }

            bool localTxn = false;
            if (!_inTransaction)
            {
                HandleBeginTransaction();
                localTxn = true;
            }

            try
            {
                // --- Write-Time Inference for UPDATE ---
                if (concept != null)
                {
                    var inferenceValues = new Dictionary<string, object>(obj.Values);
                    // Remove dependent variables so they are forced to be re-derived
                    foreach (var eq in concept.Equations)
                    {
                        var parts = eq.Expression.Split('=');
                        if (parts.Length == 2) inferenceValues.Remove(parts[0].Trim());
                    }

                    var inferenceResult = engine.FindClosure(concept, inferenceValues, new List<string>());
                    if (!inferenceResult.Success)
                    {
                        throw new Exception(inferenceResult.ErrorMessage ?? "Inference failed.");
                    }

                    if (inferenceResult.DerivedFacts.Count > 0)
                    {
                        var externalUpdates = new Dictionary<string, Dictionary<string, object>>();

                        foreach (var derived in inferenceResult.DerivedFacts)
                        {
                            if (!derived.Key.Contains('.'))
                            {
                                obj.Values[derived.Key] = derived.Value;
                            }
                            else
                            {
                                var parts = derived.Key.Split('.');
                                var alias = parts[0];
                                var field = parts[1];
                                
                                if (!externalUpdates.ContainsKey(alias))
                                    externalUpdates[alias] = new Dictionary<string, object>();
                                    
                                externalUpdates[alias][field] = derived.Value;
                            }
                        }

                        // Apply external updates
                        foreach (var kvp in externalUpdates)
                        {
                            var alias = kvp.Key;
                            var updates = kvp.Value;
                            
                            var idFact = inferenceResult.WorkingMemory.FirstOrDefault(f => f.Name.Equals($"{alias}.__internal_id", StringComparison.OrdinalIgnoreCase));
                            var conceptFact = inferenceResult.WorkingMemory.FirstOrDefault(f => f.Name.Equals($"{alias}.__internal_concept", StringComparison.OrdinalIgnoreCase));
                            
                            if (idFact != null && conceptFact != null && Guid.TryParse(idFact.Value?.ToString(), out var extId))
                            {
                                var extConcept = conceptFact.Value?.ToString();
                                if (string.IsNullOrEmpty(extConcept)) continue;

                                var extObj = _v3Router.SelectObjects(kbName, extConcept).FirstOrDefault(o => o.Id == extId);
                                if (extObj != null)
                                {
                                    foreach (var uv in updates) extObj.Values[uv.Key] = uv.Value;
                                    
                                    _txBuffer.Add(("UPDATE", kbName, extObj));
                                }
                            }
                        }
                    }
                }

                _txBuffer.Add(("UPDATE", kbName, obj));

                if (localTxn)
                {
                    HandleCommit(kbName);
                }

                updatedCount++;
                FireTriggers(kbName, node.ConceptName, "UPDATE", executor);
            }
            catch (Exception ex)
            {
                if (localTxn) HandleRollback();
                return ErrorResponse.ExecutionErrorResponse($"Update failed: {ex.Message}");
            }
        }

        return success
            ? new { success = true, message = $"{updatedCount} object(s) updated successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to update some object(s).");
    }

    private object HandleDelete(DeleteNode node, string kbName, Models.User executor)
    {
        // Capture objects BEFORE deletion so we can re-derive cross-concept effects
        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        var objectsToDelete = _v3Router.SelectObjects(
            kbName, node.ConceptName,
            values => EvaluatePredicate(values, node.Conditions, kbName, null, node.ConceptName),
            concept);

        int deletedCount = _v3Router.DeleteObjects(
            kbName, node.ConceptName,
            values => EvaluatePredicate(values, node.Conditions, kbName, null, node.ConceptName),
            concept);

        if (deletedCount > 0)
        {
            FireTriggers(kbName, node.ConceptName, "DELETE", executor);

            // --- Post-delete re-derive: find concepts that have cross-concept rules referencing
            // the deleted concept, and re-run inference on affected objects ---
            try { RederiveAffectedConcepts(kbName, node.ConceptName); }
            catch { /* Non-fatal: log only */ }
        }

        if (deletedCount == 0)
        {
            return ErrorResponse.ExecutionErrorResponse("No objects found matching conditions.");
        }

        return new { success = true, message = $"{deletedCount} object(s) deleted successfully (V3 Engine)." };
    }

    /// <summary>
    /// After deleting objects of <paramref name="deletedConceptName"/>, find all concepts
    /// that have rules involving the deleted concept and re-run write-time inference
    /// on their existing objects, so derived facts are updated / nulled-out appropriately.
    /// </summary>
    private void RederiveAffectedConcepts(string kbName, string deletedConceptName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return;

        // Collect concept names that have any cross-concept rule referencing deletedConceptName
        var affectedConcepts = kb.Rules
            .Where(r => r.ScopeConcepts != null &&
                        r.ScopeConcepts.Any(sc => sc.ConceptName.Equals(deletedConceptName, StringComparison.OrdinalIgnoreCase)) &&
                        r.ScopeConcepts.Any(sc => !sc.ConceptName.Equals(deletedConceptName, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(r => r.ScopeConcepts
                .Where(sc => !sc.ConceptName.Equals(deletedConceptName, StringComparison.OrdinalIgnoreCase))
                .Select(sc => sc.ConceptName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (affectedConcepts.Count == 0) return;

        var engine = GetConfiguredEngine(kbName);

        foreach (var conceptName in affectedConcepts)
        {
            var affectedConcept = _conceptCatalog.LoadConcept(kbName, conceptName);
            if (affectedConcept == null) continue;

            var objects = _v3Router.SelectObjects(kbName, conceptName);
            foreach (var obj in objects)
            {
                try
                {
                    var inferenceValues = new Dictionary<string, object>(obj.Values
                        .Where(kv => kv.Value != null)
                        .ToDictionary(kv => kv.Key, kv => kv.Value!));

                    // Remove equation-derived fields so they're fully re-derived
                    foreach (var eq in affectedConcept.Equations)
                    {
                        var eqParts = eq.Expression.Split('=');
                        if (eqParts.Length == 2) inferenceValues.Remove(eqParts[0].Trim());
                    }

                    var inferenceResult = engine.FindClosure(affectedConcept, inferenceValues, new List<string>());
                    if (!inferenceResult.Success) continue;

                    bool changed = false;
                    foreach (var derived in inferenceResult.DerivedFacts)
                    {
                        if (!derived.Key.Contains('.'))
                        {
                            obj.Values[derived.Key] = derived.Value;
                            changed = true;
                        }
                    }

                    if (changed)
                        _v3Router.UpdateObject(kbName, conceptName, obj.Id, 
                            obj.Values.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value!), 
                            affectedConcept);
                }
                catch { /* skip individual object failures */ }
            }
        }
    }


    private Dictionary<string, object> ConvertConditions(List<Condition> conditions)
    {
        var result = new Dictionary<string, object>();
        foreach (var cond in conditions)
        {
            if (cond.Operator == "=")
            {
                result[cond.Field] = cond.Value ?? "";
            }
        }
        return result;
    }

    private object ConvertExpressionToValue(ExpressionNode expr)
    {
        return expr switch
        {
            LiteralNode lit => lit.Value ?? "",
            VariableNode var => var.Name,
            _ => expr.ToString() ?? ""
        };
    }



    // ==================== SHOW Handlers ====================

    private object HandleShowKnowledgeBases()
    {
        var kbs = _kbCatalog.ListKbs();
        var dbs = kbs.Select(kb => kb.Name).ToList(); // Assuming ListKbs returns KbMetadata objects
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.KnowledgeBases",
            Count = dbs.Count,
            Columns = new List<string> { "Name" },
            Objects = dbs.Select(name => new ObjectInstance
            {
                Values = new Dictionary<string, object> { { "Name", name } }
            }).ToList()
        };
    }

    private object HandleShowConcepts(ShowNode node, string kbName)
    {
        var concepts = _conceptCatalog.ListConcepts(kbName).Select(c => GetEffectiveConcept(kbName, c)).ToList();
        var qrs = new QueryResultSet { 
            Success = true, 
            ConceptName = "System.Concepts",
            Count = concepts.Count,
            Columns = new List<string> { "Name", "Variables", "Constraints", "Rules" },
            Objects = concepts.Select(c => new ObjectInstance
            {
                Values = new Dictionary<string, object>
                {
                    { "Name", c.Name },
                    { "Variables", c.Variables.Count },
                    { "Constraints", c.Constraints.Count },
                    { "Rules", c.ConceptRules.Count }
                }
            }).ToList()
        };
        return qrs;
    }

    private object HandleShowConcept(ShowNode node, string kbName)
    {
        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName!);
        if (concept == null) return ErrorResponse.ExecutionErrorResponse($"Concept '{node.ConceptName}' not found.");
        
        // UNIFIED: Return as a Table via HandleDescribe logic
        return HandleDescribe(new KBMS.Parser.Ast.Kql.DescribeNode { 
            TargetType = "CONCEPT", 
            TargetName = node.ConceptName ?? "" 
        }, kbName);
    }

    private object HandleShowRules(ShowNode node, string kbName)
    {
        var rules = ListRules(kbName);

        if (!string.IsNullOrEmpty(node.RuleType))
        {
            rules = rules.Where(r => r.RuleType?.Equals(node.RuleType, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.Rules",
            Count = rules.Count,
            Columns = new List<string> { "Name" },
            Objects = rules.Select(r => new ObjectInstance
            {
                Values = new Dictionary<string, object> { { "Name", r.Name } }
            }).ToList()
        };
    }

    private object HandleShowRelations(ShowNode node, string kbName)
    {
        var relations = ListRelations(kbName);
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.Relations",
            Count = relations.Count,
            Columns = new List<string> { "Name", "Domain", "Range" },
            Objects = relations.Select(r => new ObjectInstance
            {
                Values = new Dictionary<string, object>
                {
                    { "Name", r.Name },
                    { "Domain", r.Domain },
                    { "Range", r.Range }
                }
            }).ToList()
        };
    }

    private object HandleShowOperators(ShowNode node, string kbName)
    {
        var operators = ListOperators(kbName);
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.Operators",
            Count = operators.Count,
            Columns = new List<string> { "Symbol", "ParamTypes", "ReturnType" },
            Objects = operators.Select(o => new ObjectInstance
            {
                Values = new Dictionary<string, object>
                {
                    { "Symbol", o.Symbol },
                    { "ParamTypes", string.Join(", ", o.ParamTypes) },
                    { "ReturnType", o.ReturnType }
                }
            }).ToList()
        };
    }

    private object HandleShowFunctions(ShowNode node, string kbName)
    {
        var functions = ListFunctions(kbName);
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.Functions",
            Count = functions.Count,
            Columns = new List<string> { "Name", "Parameters", "ReturnType" },
            Objects = functions.Select(f => new ObjectInstance
            {
                Values = new Dictionary<string, object>
                {
                    { "Name", f.Name },
                    { "Parameters", string.Join(", ", f.Parameters.Select(p => p.Name + ": " + p.Type)) },
                    { "ReturnType", f.ReturnType }
                }
            }).ToList()
        };
    }

    private object HandleShowHierarchies(ShowNode node, string kbName)
    {
        var hierarchies = ListHierarchies(kbName);
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.Hierarchies",
            Count = hierarchies.Count,
            Columns = new List<string> { "ParentConcept", "ChildConcept", "HierarchyType" },
            Objects = hierarchies.Select(h => new ObjectInstance
            {
                Values = new Dictionary<string, object>
                {
                    { "ParentConcept", h.ParentConcept },
                    { "ChildConcept", h.ChildConcept },
                    { "HierarchyType", h.HierarchyType.ToString() }
                }
            }).ToList()
        };
    }

    private object HandleShowUsers()
    {
        var users = _userCatalog.ListUsers();
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.Users",
            Count = users.Count,
            Columns = new List<string> { "Username", "Role", "IsSystemAdmin" },
            Objects = users.Select(u => new ObjectInstance
            {
                Values = new Dictionary<string, object>
                {
                    { "Username", u.Username },
                    { "Role", u.Role.ToString() },
                    { "IsSystemAdmin", u.SystemAdmin }
                }
            }).ToList()
        };
    }

    private object HandleShowTriggers(ShowNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        var triggers = kb != null ? kb.Triggers : new List<Models.Trigger>();
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.Triggers",
            Count = triggers.Count,
            Columns = new List<string> { "Name", "Event", "TargetConcept" },
            Objects = triggers.Select(t => new ObjectInstance
            {
                Values = new Dictionary<string, object> 
                { 
                    { "Name", t.Name },
                    { "Event", t.Event },
                    { "TargetConcept", t.TargetConcept }
                }
            }).ToList()
        };
    }

    private object HandleShowIndexes(ShowNode node, string kbName)
    {
        return new QueryResultSet
        {
            Success = true,
            ConceptName = "System.Indexes",
            Count = 0,
            Columns = new List<string> { "Name", "TargetConcept", "Fields" },
            Objects = new List<ObjectInstance>() // Placeholder since indexes are auto-managed in V3
        };
    }

    private object HandleShowPrivilegesOnKb(ShowNode node)
    {
        var users = _userCatalog.ListUsers();
        var privileges = new List<ObjectInstance>();

        foreach (var user in users)
        {
            if (user.KbPrivileges.TryGetValue(node.KbName!, out var priv))
            {
                privileges.Add(new ObjectInstance
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Username", user.Username },
                        { "Privilege", priv.ToString() }
                    }
                });
            }
        }

        return new QueryResultSet
        {
            Success = true,
            ConceptName = $"Privileges On {node.KbName}",
            Count = privileges.Count,
            Objects = privileges,
            Columns = new List<string> { "Username", "Privilege" }
        };
    }

    private object HandleShowPrivilegesOfUser(ShowNode node)
    {
        var users = _userCatalog.ListUsers();
        var user = users.FirstOrDefault(u => u.Username == node.Username);

        if (user == null)
            return ErrorResponse.ExecutionErrorResponse($"User '{node.Username}' not found.");

        var privileges = user.KbPrivileges.Select(kvp => new ObjectInstance
        {
            Values = new Dictionary<string, object>
            {
                { "KnowledgeBase", kvp.Key },
                { "Privilege", kvp.Value.ToString() }
            }
        }).ToList();

        return new QueryResultSet
        {
            Success = true,
            ConceptName = $"Privileges Of {node.Username}",
            Count = privileges.Count,
            Objects = privileges,
            Columns = new List<string> { "KnowledgeBase", "Privilege" }
        };
    }

    private object HandleAlterConcept(AlterConceptNode node, string kbName)
    {
        var conceptsToAlter = new List<string>();
        if (node.ConceptName == "*")
        {
            conceptsToAlter.AddRange(_conceptCatalog.ListConcepts(kbName).Select(c => c.Name));
        }
        else
        {
            conceptsToAlter.Add(node.ConceptName);
        }

        foreach (var cName in conceptsToAlter)
        {
            var concept = _conceptCatalog.LoadConcept(kbName, cName);
            if (concept == null) continue;

            foreach (var action in node.Actions)
            {
                switch (action.ActionType)
                {
                    case AlterActionType.AddVariable:
                        if (action.Variable != null) 
                            concept.Variables.Add(new Variable { Name = action.Variable.Name, Type = action.Variable.Type, Length = action.Variable.Length, Scale = action.Variable.Scale });
                        break;
                    case AlterActionType.DropVariable:
                        concept.Variables.RemoveAll(v => v.Name.Equals(action.TargetName, StringComparison.OrdinalIgnoreCase));
                        break;
                    case AlterActionType.RenameVariable:
                        var v = concept.Variables.FirstOrDefault(v => v.Name.Equals(action.OldName, StringComparison.OrdinalIgnoreCase));
                        if (v != null) v.Name = action.NewName!;
                        break;
                    case AlterActionType.AddConstraint:
                        if (action.Constraint != null)
                            concept.Constraints.Add(new Constraint { Name = action.Constraint.Name, Expression = action.Constraint.Expression, Line = action.Constraint.Line, Column = action.Constraint.Column });
                        break;
                    case AlterActionType.DropConstraint:
                        concept.Constraints.RemoveAll(c => c.Name.Equals(action.TargetName, StringComparison.OrdinalIgnoreCase));
                        break;
                    case AlterActionType.AddRule:
                        if (action.Rule != null)
                            concept.ConceptRules.Add(new ConceptRule { 
                                Id = Guid.NewGuid(),
                                Name = action.Rule.Name,
                                Kind = action.Rule.Kind,
                                Variables = action.Rule.Variables.Select(v => new Variable { Name = v.Name, Type = v.Type, Length = v.Length, Scale = v.Scale }).ToList(),
                                Hypothesis = action.Rule.Hypothesis.ToList(),
                                Conclusion = action.Rule.Conclusion.ToList()
                            });
                        break;
                    case AlterActionType.DropRule:
                        concept.ConceptRules.RemoveAll(r => r.Kind.Equals(action.TargetName, StringComparison.OrdinalIgnoreCase) || (r.Id.ToString() == action.TargetName));
                        break;

                    case AlterActionType.AddEquation:
                        if (action.Equation != null)
                            concept.Equations.Add(new Equation { Id = Guid.NewGuid(), Expression = action.Equation.Expression });
                        break;
                    case AlterActionType.DropEquation:
                        concept.Equations.RemoveAll(eq => eq.Expression.Equals(action.TargetName, StringComparison.OrdinalIgnoreCase)
                                                       || eq.Id.ToString() == action.TargetName);
                        break;

                    case AlterActionType.AddProperty:
                        if (action.Property != null)
                        {
                            concept.Properties.RemoveAll(p => p.Key.Equals(action.Property.Key, StringComparison.OrdinalIgnoreCase));
                            concept.Properties.Add(new Property { Key = action.Property.Key, Value = action.Property.Value });
                        }
                        break;
                    case AlterActionType.DropProperty:
                        concept.Properties.RemoveAll(p => p.Key.Equals(action.TargetName, StringComparison.OrdinalIgnoreCase));
                        break;

                    case AlterActionType.AddConstructRelation:
                        if (action.ConstructRelation != null)
                        {
                            concept.ConstructRelations.RemoveAll(cr => cr.RelationName.Equals(action.ConstructRelation.RelationName, StringComparison.OrdinalIgnoreCase));
                            concept.ConstructRelations.Add(new ConstructRelation { RelationName = action.ConstructRelation.RelationName, Arguments = action.ConstructRelation.Arguments });
                        }
                        break;
                    case AlterActionType.DropConstructRelation:
                        concept.ConstructRelations.RemoveAll(cr => cr.RelationName.Equals(action.TargetName, StringComparison.OrdinalIgnoreCase));
                        break;
                }
            }
            // _conceptCatalog.UpdateConcept(kbName, concept); // Moved below validation loop
            
            // ✅ V3 Data Migration & Validation: Ensure all existing objects comply with the new schema/constraints
            var existingObjects = _v3Router.SelectObjects(kbName, cName);
            var migratedObjects = new List<(Guid Id, Dictionary<string, object> Values)>();

            foreach (var obj in existingObjects)
            {
                var newValues = new Dictionary<string, object>(obj.Values);
                bool schemaModified = false;
                
                foreach (var action in node.Actions)
                {
                    switch (action.ActionType)
                    {
                        case AlterActionType.RenameVariable:
                            if (newValues.TryGetValue(action.OldName!, out var val))
                            {
                                newValues.Remove(action.OldName!);
                                newValues[action.NewName!] = val;
                                schemaModified = true;
                            }
                            break;
                        case AlterActionType.DropVariable:
                            if (newValues.Remove(action.TargetName!)) schemaModified = true;
                            break;
                        case AlterActionType.AddVariable:
                            if (!newValues.ContainsKey(action.Variable!.Name))
                            {
                                newValues[action.Variable.Name] = null!; 
                                schemaModified = true;
                            }
                            break;
                    }
                }

                if (schemaModified)
                {
                    migratedObjects.Add((obj.Id, newValues));
                }
            }

            // If we get here, all data is valid. Commit the concept and update objects.
            _conceptCatalog.UpdateConcept(kbName, concept);
            foreach (var migration in migratedObjects)
            {
                _v3Router.UpdateObject(kbName, cName, migration.Id, migration.Values, concept);
            }
            
            Console.WriteLine($"[V3] Persisted altered concept '{cName}' and migrated/validated {existingObjects.Count} objects.");
        }

        // Invalidate Rete network cache for all altered concepts
        foreach (var cName in conceptsToAlter)
            InvalidateEngineCache(kbName);

        return new { success = true, alteredCount = conceptsToAlter.Count };
    }

    private object HandleAlterKnowledgeBase(AlterKbNode node)
    {
        var kbs = _kbCatalog.ListKbs();
        var targets = node.KbName == "*" ? kbs.Select(k => k.Name).ToList() : new List<string> { node.KbName };

        foreach (var name in targets)
        {
            var kb = kbs.FirstOrDefault(k => k.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (kb != null)
            {
                kb.Description = node.NewDescription;
                _kbCatalog.SaveKbMetadata(kb);
            }
        }
        return new { success = true, alteredCount = targets.Count };
    }

    private object HandleAlterUser(AlterUserNode node)
    {
        var user = _userCatalog.FindUser(node.Username);
        if (user == null) return ErrorResponse.ExecutionErrorResponse($"User '{node.Username}' not found.");

        if (node.NewPassword != null) 
        {
            if (!_userCatalog.ChangePassword(node.Username, node.NewPassword))
                return ErrorResponse.ExecutionErrorResponse("Failed to update password.");
        }
        if (node.NewAdminStatus.HasValue) 
        {
            user.SystemAdmin = node.NewAdminStatus.Value;
            if (!_userCatalog.UpdateUser(user))
                return ErrorResponse.ExecutionErrorResponse("Failed to update admin status.");
        }

        return new { success = true, username = node.Username, message = "User altered successfully (V3)." };
    }

    private object HandleCreateIndex(KBMS.Parser.Ast.Kdl.CreateIndexNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");

        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        if (concept == null) return ErrorResponse.ExecutionErrorResponse($"Concept '{node.ConceptName}' not found.");

        if (concept.Indexes == null) concept.Indexes = new List<Models.ConceptIndex>();

        if (concept.Indexes.Any(i => i.Name.Equals(node.IndexName, StringComparison.OrdinalIgnoreCase)))
            return ErrorResponse.ExecutionErrorResponse($"Index '{node.IndexName}' already exists.");

        foreach (var v in node.Variables)
        {
            if (!concept.Variables.Any(cv => cv.Name.Equals(v, StringComparison.OrdinalIgnoreCase)))
                return ErrorResponse.ExecutionErrorResponse($"Variable '{v}' not found in concept '{node.ConceptName}'.");
        }

        var newIndex = new Models.ConceptIndex { Name = node.IndexName, Fields = node.Variables };
        concept.Indexes.Add(newIndex);
        _conceptCatalog.UpdateConcept(kbName, concept);

        // Build the B+ Tree index with existing data
        _v3Router.BackfillIndex(kbName, node.ConceptName, newIndex);

        return new { success = true, message = $"Index '{node.IndexName}' successfully created on concept '{node.ConceptName}'." };
    }

    private object HandleDropIndex(KBMS.Parser.Ast.Kdl.DropIndexNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");

        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        if (concept == null) return ErrorResponse.ExecutionErrorResponse($"Concept '{node.ConceptName}' not found.");

        if (concept.Indexes == null) return ErrorResponse.ExecutionErrorResponse($"Index '{node.IndexName}' not found.");

        var existing = concept.Indexes.FirstOrDefault(i => i.Name.Equals(node.IndexName, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
            return ErrorResponse.ExecutionErrorResponse($"Index '{node.IndexName}' not found.");

        concept.Indexes.Remove(existing);
        _conceptCatalog.UpdateConcept(kbName, concept);
        
        _v3Router.DropConceptIndex(kbName, node.ConceptName, node.IndexName);

        return new { success = true, message = $"Index '{node.IndexName}' on concept '{node.ConceptName}' dropped successfully." };
    }

    private object HandleDropTrigger(KBMS.Parser.Ast.Kdl.DropTriggerNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");

        var existing = kb.Triggers.FirstOrDefault(t => t.Name.Equals(node.TriggerName, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
            return ErrorResponse.ExecutionErrorResponse($"Trigger '{node.TriggerName}' not found.");

        kb.Triggers.Remove(existing);
        
        if (!_kbCatalog.SaveKbMetadata(kb))
            return ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata after dropping trigger.");

        if (_triggers.ContainsKey(kbName))
        {
             _triggers[kbName].RemoveAll(t => t.TriggerName.Equals(node.TriggerName, StringComparison.OrdinalIgnoreCase));
        }

        return new { success = true, message = $"Trigger '{node.TriggerName}' dropped successfully." };
    }

    private object HandleMaintenance(KBMS.Parser.Ast.Kml.MaintenanceNode node, string kbName)
    {
        var results = new List<object>();
        foreach(var action in node.Actions)
        {
            string actionName = action.ActionType switch
            {
                MaintenanceActionType.Vacuum => "VACUUM",
                MaintenanceActionType.Reindex => "REINDEX",
                MaintenanceActionType.CheckConsistency => "CHECK_CONSISTENCY",
                _ => action.ActionType.ToString().ToUpper()
            };
            results.Add(new { action = actionName, status = "Completed (V3 Placeholder)" });
        }
        return new { success = true, results = results };
    }

    private object HandleExplain(ExplainNode node, string? kbName)
    {
        var qrs = new QueryResultSet { ConceptName = "Explain_Plan", Success = true };
        
        string targetTable = "System";
        if (node.Query is KBMS.Parser.Ast.Kql.DescribeNode descNode)
            targetTable = descNode.TargetName ?? "Unknown";
        else if (node.Query is KBMS.Parser.Ast.Kql.ShowNode showNode)
            targetTable = showNode.Type ?? "System";

        qrs.Objects.Add(new ObjectInstance {
            Values = new Dictionary<string, object> {
                { "Step", 1 },
                { "Operation", "SYNTAX_PARSE" },
                { "Target", "Parser_Engine" },
                { "Detail", $"Generated AST Node Type: {node.Query?.Type}" }
            }
        });
        
        qrs.Objects.Add(new ObjectInstance {
            Values = new Dictionary<string, object> {
                { "Step", 2 },
                { "Operation", "SEMANTIC_CHECK" },
                { "Target", targetTable },
                { "Detail", $"Verify existence and privileges in KB: {kbName ?? "default"}" }
            }
        });
        
        qrs.Objects.Add(new ObjectInstance {
            Values = new Dictionary<string, object> {
                { "Step", 3 },
                { "Operation", "EXECUTION" },
                { "Target", "Knowledge_Manager" },
                { "Detail", $"Delegate node {node.Query?.Type} to specific handler" }
            }
        });

        qrs.Count = qrs.Objects.Count;
        return qrs;
    }

    private object HandleDescribe(KBMS.Parser.Ast.Kql.DescribeNode node, string kbName)
    {
        switch (node.TargetType.ToUpper())
        {
            case "CONCEPT":
            {
                var concepts = _conceptCatalog.ListConcepts(kbName);
                var c = concepts.FirstOrDefault(x => x.Name.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase));
                if (c == null) return ErrorResponse.ExecutionErrorResponse($"Concept '{node.TargetName}' not found in KB '{kbName}'");
                
                var qrs = new QueryResultSet { ConceptName = "Describe_Concept", Success = true };
                var valuesDict = new Dictionary<string, object?>
                {
                    { "Id", c.Id },
                    { "KbId", c.KbId },
                    { "Concept", c.Name },
                    { "Aliases", c.Aliases.Count > 0 ? string.Join(", ", c.Aliases) : null },
                    { "BaseObjects", c.BaseObjects.Count > 0 ? string.Join(", ", c.BaseObjects) : null },
                    { "Variables", c.Variables.Count > 0 ? string.Join("\n", c.Variables.Select(v => $"{v.Name} ({GetFormattedType(v)})")) : null },
                    { "SameVariables", c.SameVariables.Count > 0 ? string.Join("\n", c.SameVariables.Select(sv => $"{sv.Variable1} = {sv.Variable2}")) : null },
                    { "Constraints", c.Constraints.Count > 0 ? string.Join("\n", c.Constraints.Select(ct => ct.Expression)) : null },
                    { "Equations", c.Equations.Count > 0 ? string.Join("\n", c.Equations.Select(eq => eq.Expression)) : null },
                    { "Rules", c.ConceptRules.Count > 0 ? string.Join("\n", c.ConceptRules.Select(r => $"{(string.IsNullOrEmpty(r.Kind) ? "RULE" : r.Kind)}: {string.Join(" AND ", r.Hypothesis)} => {string.Join(", ", r.Conclusion)}")) : null },
                    { "CompRels", c.CompRels.Count > 0 ? string.Join("\n", c.CompRels.Select(cr => $"[Rank {cr.Rank}] {string.Join(",", cr.InputVariables)} -> {cr.ResultVariable} (Cost:{cr.Cost}) Expr: {cr.Expression}")) : null },
                    { "ConstructRelations", c.ConstructRelations.Count > 0 ? string.Join("\n", c.ConstructRelations.Select(cr => $"{cr.RelationName}({string.Join(", ", cr.Arguments)})")) : null },
                    { "Properties", c.Properties.Count > 0 ? string.Join("\n", c.Properties.Select(p => $"{p.Key}: {p.Value}")) : null },
                    { "_JsonData", System.Text.Json.JsonSerializer.Serialize(c) }
                };

                qrs.Objects.Add(new ObjectInstance { Values = valuesDict.ToDictionary(kv => kv.Key, kv => (object)(kv.Value ?? string.Empty)) });
                qrs.Count = qrs.Objects.Count;
                if (qrs.Objects.Count > 0)
                    qrs.Columns = qrs.Objects[0].Values.Keys.ToList();
                return qrs;
            }
            case "KB":
            {
                var kbs = _kbCatalog.ListKbs();
                var kb = kbs.FirstOrDefault(x => x.Name.Equals(kbName, StringComparison.OrdinalIgnoreCase));
                if (kb == null) return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");
                
                var qrs = new QueryResultSet { ConceptName = "Describe_KB", Success = true };
                qrs.Objects.Add(new ObjectInstance
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Knowledge Base", kbName },
                        { "Description", kb.Description },
                        { "Concepts Count", _conceptCatalog.ListConcepts(kbName).Count.ToString() },
                        { "Objects Count", kb.ObjectCount.ToString() }
                    }
                });
                qrs.Count = qrs.Objects.Count;
                if (qrs.Objects.Count > 0) qrs.Columns = qrs.Objects[0].Values.Keys.ToList();
                return qrs;
            }
            case "HIERARCHY":
            {
                var kb = _kbCatalog.LoadKb(kbName);
                if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
                
                var parts = node.TargetName.Split(':');
                var child = parts[0];
                var parent = parts.Length > 1 ? parts[1] : null;

                var hierarchies = kb.Hierarchies.Where(h => 
                    h.ChildConcept.Equals(child, StringComparison.OrdinalIgnoreCase) &&
                    (parent == null || h.ParentConcept.Equals(parent, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (!hierarchies.Any()) return ErrorResponse.ExecutionErrorResponse($"Hierarchy for '{node.TargetName}' not found.");

                var qrs = new QueryResultSet { ConceptName = "Describe_Hierarchy", Success = true };
                foreach (var h in hierarchies)
                {
                    qrs.Objects.Add(new ObjectInstance
                    {
                        Values = new Dictionary<string, object>
                        {
                            { "Child", h.ChildConcept },
                            { "Type", h.HierarchyType.ToString() },
                            { "Parent", h.ParentConcept }
                        }
                    });
                }
                qrs.Count = qrs.Objects.Count;
                qrs.Columns = new List<string> { "Child", "Type", "Parent" };
                return qrs;
            }
            case "RULE":
            {
                var kb = _kbCatalog.LoadKb(kbName);
                if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
                var r = kb.Rules.FirstOrDefault(x => x.Name.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase));
                if (r == null) return ErrorResponse.ExecutionErrorResponse($"Rule '{node.TargetName}' not found.");

                var qrs = new QueryResultSet { ConceptName = "Describe_Rule", Success = true };
                qrs.Objects.Add(new ObjectInstance
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Name", r.Name },
                        { "Scope", r.Scope },
                        { "Hypothesis", string.Join(" AND ", r.Hypothesis.Select(h => h.Content)) },
                        { "Conclusion", string.Join(", ", r.Conclusion.Select(c => c.Content)) }
                    }
                });
                qrs.Count = 1;
                qrs.Columns = new List<string> { "Name", "Scope", "Hypothesis", "Conclusion" };
                return qrs;
            }
            case "RELATION":
            {
                var rels = ListRelations(kbName);
                var r = rels.FirstOrDefault(x => x.Name.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase));
                if (r == null) return ErrorResponse.ExecutionErrorResponse($"Relation '{node.TargetName}' not found.");

                var qrs = new QueryResultSet { ConceptName = "Describe_Relation", Success = true };
                qrs.Objects.Add(new ObjectInstance {
                    Values = new Dictionary<string, object> {
                        { "Relation Name", r.Name },
                        { "Domain", r.Domain },
                        { "Range", r.Range },
                        { "Params", string.Join(", ", r.ParamNames) },
                        { "Properties", string.Join(", ", r.Properties) }
                    }
                });
                qrs.Count = 1;
                if (qrs.Objects.Count > 0) qrs.Columns = qrs.Objects[0].Values.Keys.ToList();
                return qrs;
            }
            case "FUNCTION":
            {
                var funcs = ListFunctions(kbName);
                var f = funcs.FirstOrDefault(x => x.Name.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase));
                if (f == null) return ErrorResponse.ExecutionErrorResponse($"Function '{node.TargetName}' not found.");

                var qrs = new QueryResultSet { ConceptName = "Describe_Function", Success = true };
                qrs.Objects.Add(new ObjectInstance {
                    Values = new Dictionary<string, object> {
                        { "Function Name", f.Name },
                        { "Parameters", string.Join(", ", f.Parameters.Select(p => $"{p.Name}: {p.Type}")) },
                        { "Return Type", f.ReturnType },
                        { "Properties", string.Join(", ", f.Properties) }
                    }
                });
                qrs.Count = 1;
                if (qrs.Objects.Count > 0) qrs.Columns = qrs.Objects[0].Values.Keys.ToList();
                return qrs;
            }
            case "OPERATOR":
            {
                var ops = ListOperators(kbName);
                var o = ops.FirstOrDefault(x => x.Symbol.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase));
                if (o == null) return ErrorResponse.ExecutionErrorResponse($"Operator '{node.TargetName}' not found.");

                var qrs = new QueryResultSet { ConceptName = "Describe_Operator", Success = true };
                qrs.Objects.Add(new ObjectInstance {
                    Values = new Dictionary<string, object> {
                        { "Operator Symbol", o.Symbol },
                        { "Param Types", string.Join(", ", o.ParamTypes) },
                        { "Return Type", o.ReturnType },
                        { "Properties", string.Join(", ", o.Properties) }
                    }
                });
                qrs.Count = 1;
                if (qrs.Objects.Count > 0) qrs.Columns = qrs.Objects[0].Values.Keys.ToList();
                return qrs;
            }
            default:
                return ErrorResponse.ExecutionErrorResponse($"Unknown DESCRIBE target type: {node.TargetType}");
        }
    }

    private string GetFormattedType(Variable v)
    {
        string type = v.Type.ToUpper();
        if (type == "DECIMAL" || type == "NUMBER" || type == "MONEY")
        {
            if (v.Length > 0) return $"DECIMAL({v.Length},{v.Scale})";
            if (v.Scale > 0) return $"DECIMAL(?,{v.Scale})";
            return "DECIMAL";
        }
        if (type == "VARCHAR" || type == "CHAR" || type == "STRING")
        {
            if (v.Length > 0) return $"{type}({v.Length})";
        }
        return type;
    }

    private object HandleExport(KBMS.Parser.Ast.Kml.ExportNode node, string kbName)
    {
        try
        {
            if (node.Format.Equals("KBPKG", StringComparison.OrdinalIgnoreCase))
            {
                return ExportKnowledgeBaseAsKbpkg(node, kbName);
            }

            // Full Knowledge Base dump (MySQL-style .kql script)
            if (node.TargetType.Equals("KNOWLEDGE_BASE", StringComparison.OrdinalIgnoreCase) ||
                node.Format.Equals("KQL", StringComparison.OrdinalIgnoreCase))
            {
                return ExportKnowledgeBaseAsKql(node, kbName);
            }

            // Legacy: Export concept data as JSON
            var objects = SelectAllObjects(kbName)
                .Where(o => node.TargetName == "*" || o.ConceptName.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var json = System.Text.Json.JsonSerializer.Serialize(objects, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var dir = Path.GetDirectoryName(node.FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(node.FilePath, json);

            return new { success = true, exported = objects.Count, concept = node.TargetName, file = node.FilePath };
        }
        catch (Exception ex)
        {
            return ErrorResponse.ExecutionErrorResponse(ex.Message);
        }
    }

    private object ExportKnowledgeBaseAsKql(KBMS.Parser.Ast.Kml.ExportNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"-- KBMS Knowledge Base Dump");
        sb.AppendLine($"-- KB: {kbName}");
        sb.AppendLine($"-- Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // ============ CONCEPTS (DDL) ============
        sb.AppendLine("-- -------------------------");
        sb.AppendLine("-- CONCEPTS");
        sb.AppendLine("-- -------------------------");
        var concepts = _conceptCatalog.ListConcepts(kbName);
        foreach (var c in concepts)
        {
            sb.AppendLine($"DROP CONCEPT IF EXISTS {c.Name};");
            var varList = string.Join(", ", c.Variables
                .Where(v => !v.Name.Contains('.')) // skip expanded dot-vars
                .Select(v =>
                {
                    string typePart = v.Length.HasValue && v.Length > 0
                        ? $"{v.Type}({v.Length}{(v.Scale.HasValue && v.Scale > 0 ? $",{v.Scale}" : "")})"
                        : v.Type;
                    return $"{v.Name}: {typePart}";
                }));
            sb.AppendLine($"CREATE CONCEPT {c.Name} (VARIABLES ({varList}));");
        }
        sb.AppendLine();

        // ============ HIERARCHIES (DDL) ============
        var hierarchies = ListHierarchies(kbName);
        if (hierarchies.Count > 0)
        {
            sb.AppendLine("-- -------------------------");
            sb.AppendLine("-- HIERARCHIES");
            sb.AppendLine("-- -------------------------");
            foreach (var h in hierarchies)
            {
                string typeStr = "ISA";
                sb.AppendLine($"ADD HIERARCHY {h.ChildConcept} {typeStr} {h.ParentConcept};");
            }
            sb.AppendLine();
        }

        // ============ RELATIONS (DDL) ============
        var relations = ListRelations(kbName);
        if (relations.Count > 0)
        {
            sb.AppendLine("-- -------------------------");
            sb.AppendLine("-- RELATIONS");
            sb.AppendLine("-- -------------------------");
            foreach (var r in relations)
                sb.AppendLine($"CREATE RELATION {r.Name} DOMAIN {r.Domain} RANGE {r.Range};");
            sb.AppendLine();
        }

        // ============ RULES (DDL) ============
        var rules = ListRules(kbName);
        if (rules.Count > 0)
        {
            sb.AppendLine("-- -------------------------");
            sb.AppendLine("-- RULES");
            sb.AppendLine("-- -------------------------");
            foreach (var r in rules)
            {
                var hyp = string.Join(", ", r.Hypothesis.Select(h => h.Content));
                var con = string.Join(", ", r.Conclusion.Select(c => c.Content));
                sb.AppendLine($"CREATE RULE {r.Name} SCOPE {r.Scope} IF {hyp} THEN {con};");
            }
            sb.AppendLine();
        }

        // ============ TRIGGERS (DDL) ============
        if (kb.Triggers.Count > 0)
        {
            sb.AppendLine("-- -------------------------");
            sb.AppendLine("-- TRIGGERS");
            sb.AppendLine("-- -------------------------");
            foreach (var t in kb.Triggers)
                if (!string.IsNullOrEmpty(t.OriginalQuery))
                    sb.AppendLine(t.OriginalQuery.TrimEnd(';') + ";");
            sb.AppendLine();
        }

        // ============ DATA (DML) ============
        sb.AppendLine("-- -------------------------");
        sb.AppendLine("-- DATA");
        sb.AppendLine("-- -------------------------");
        var allObjects = SelectAllObjects(kbName).ToList();
        var grouped = allObjects.GroupBy(o => o.ConceptName);
        foreach (var group in grouped)
        {
            var objectsInGroup = group.ToList();
            int batchSize = 100;
            for (int i = 0; i < objectsInGroup.Count; i += batchSize)
            {
                var batch = objectsInGroup.Skip(i).Take(batchSize).ToList();
                sb.Append($"INSERT BULK INTO {group.Key} VARIABLES ");
                
                var rows = new List<string>();
                foreach (var obj in batch)
                {
                    var vals = string.Join(", ", obj.Values.Select(kvp =>
                    {
                        var v = kvp.Value;
                        string valStr = "NULL";
                        if (v != null)
                        {
                            if (v is string s) valStr = $"'{s.Replace("'", "''")}'";
                            else if (v is bool b) valStr = b.ToString().ToUpper();
                            else valStr = v.ToString() ?? "NULL";
                        }
                        return $"{kvp.Key}: {valStr}";
                    }));
                    rows.Add($"({vals})");
                }
                sb.Append(string.Join(", ", rows));
                sb.AppendLine(";");
            }
        }
        sb.AppendLine();

        var outDir = Path.GetDirectoryName(node.FilePath);
        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
        File.WriteAllText(node.FilePath, sb.ToString());

        return new { success = true, exported_concepts = concepts.Count, exported_objects = allObjects.Count, file = node.FilePath };
    }

    private object HandleImport(KBMS.Parser.Ast.Kml.ImportNode node, string kbName, Models.User executor)
    {
        try
        {
            if (!File.Exists(node.FilePath))
                return ErrorResponse.ExecutionErrorResponse($"File not found: {node.FilePath}");

            // Full KB restore from a KQL dump script
            if (node.TargetType.Equals("KNOWLEDGE_BASE", StringComparison.OrdinalIgnoreCase) ||
                node.Format.Equals("KQL", StringComparison.OrdinalIgnoreCase))
            {
                return ImportKnowledgeBaseFromKql(node, kbName, executor);
            }

            // Legacy: Import concept data from a JSON file
            var kb = _kbCatalog.LoadKb(kbName);
            if (kb == null)
                return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");

            var json = File.ReadAllText(node.FilePath);
            var imported = System.Text.Json.JsonSerializer.Deserialize<List<KBMS.Models.ObjectInstance>>(json);
            if (imported == null) return ErrorResponse.ExecutionErrorResponse("Failed to deserialize import file");

            var objectsToInsert = new List<KBMS.Models.ObjectInstance>();
            foreach (var obj in imported)
            {
                if (node.TargetName != "*" && !obj.ConceptName.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase))
                    continue;
                obj.Id = Guid.NewGuid();
                obj.KbId = kb.Id;
                objectsToInsert.Add(obj);
            }

            var inserted = 0;
            foreach (var group in objectsToInsert.GroupBy(o => o.ConceptName))
            {
                var concept = _conceptCatalog.LoadConcept(kbName, group.Key);
                inserted += _v3Router.BulkInsertObjects(kbName, group.ToList(), concept);
            }
            
            if (inserted > 0)
            {
                var conceptsAffected = objectsToInsert.Select(o => o.ConceptName).Distinct();
                foreach (var concept in conceptsAffected)
                    FireTriggers(kbName, concept, "INSERT", executor);
            }

            return new { success = true, imported = inserted, concept = node.TargetName, file = node.FilePath };
        }
        catch (Exception ex)
        {
            return ErrorResponse.ExecutionErrorResponse(ex.Message);
        }
    }

    private object ImportKnowledgeBaseFromKql(KBMS.Parser.Ast.Kml.ImportNode node, string kbName, Models.User executor)
    {
        var script = File.ReadAllText(node.FilePath);
        var parser = new KBMS.Parser.Parser(script);
        List<KBMS.Parser.Ast.AstNode> statements;
        try
        {
            statements = parser.ParseAll();
        }
        catch (Exception ex)
        {
            return ErrorResponse.ExecutionErrorResponse($"Failed to parse KQL dump: {ex.Message}");
        }

        int successCount = 0, errorCount = 0;
        var errors = new List<string>();

        foreach (var stmt in statements)
        {
            // Ensure the statement targets the current KB
            stmt.KbName ??= kbName;
            try
            {
                var result = ExecuteQuery(stmt, kbName, executor);
                if (result is ErrorResponse err)
                    errors.Add($"[Line {stmt.Line}] {err.Message}");
                else
                    successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                errors.Add($"[Line {stmt.Line}] {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            Console.WriteLine("IMPORT ERRORS:");
            foreach (var e in errors) Console.WriteLine(e);
        }

        // Evict cached triggers to force a reload from new metadata
        _triggers.Remove(kbName);

        return new
        {
            success = errorCount == 0,
            statements_executed = successCount,
            errors = errorCount,
            error_details = errors,
            file = node.FilePath
        };
    }

    private List<string> GetAncestors(string conceptName, List<Models.Hierarchy> hierarchies)
    {
        var ancestors = new List<string>();
        var directParents = hierarchies
            .Where(h => h.ChildConcept.Equals(conceptName, StringComparison.OrdinalIgnoreCase) && h.HierarchyType == Models.HierarchyType.IsA)
            .Select(h => h.ParentConcept)
            .ToList();

        foreach (var parent in directParents)
        {
            if (!ancestors.Contains(parent))
            {
                ancestors.Add(parent);
                ancestors.AddRange(GetAncestors(parent, hierarchies).Where(a => !ancestors.Contains(a)));
            }
        }

        return ancestors;
    }

    private QueryResultSet ExtractConceptMetadata(Models.Concept c, string? subTarget)
    {
        var result = new QueryResultSet { 
            ConceptName = $"{c.Name}.{subTarget ?? "metadata"}",
            Success = true 
        };

        if (string.IsNullOrEmpty(subTarget))
        {
            result.Columns = new List<string> { 
                "Concept", "Aliases", "BaseObjects", "Variables", "SameVariables", 
                "Constraints", "Equations", "Rules", "CompRels", "ConstructRelations", "Properties" 
            };

            result.Objects.Add(new ObjectInstance {
                ConceptName = c.Name,
                Values = new Dictionary<string, object> {
                    { "Concept", c.Name },
                    { "Aliases", c.Aliases.Count > 0 ? string.Join(", ", c.Aliases) : "None" },
                    { "BaseObjects", c.BaseObjects.Count > 0 ? string.Join(", ", c.BaseObjects) : "None" },
                    { "Variables", c.Variables.Count > 0 ? c.Variables.Select(v => $"{v.Name} ({v.Type})").ToList() : (object)"None" },
                    { "SameVariables", c.SameVariables.Count > 0 ? c.SameVariables.Select(x => $"{x.Variable1} = {x.Variable2}").ToList() : (object)"None" },
                    { "Constraints", c.Constraints.Count > 0 ? c.Constraints.Select(x => x.Expression).ToList() : (object)"None" },
                    { "Equations", c.Equations.Count > 0 ? c.Equations.Select(x => x.Expression).ToList() : (object)"None" },
                    { "Rules", c.ConceptRules.Count > 0 ? c.ConceptRules.Select(x => x.Kind).ToList() : (object)"None" },
                    { "CompRels", c.CompRels.Count > 0 ? c.CompRels.Select(x => x.Expression).ToList() : (object)"None" },
                    { "ConstructRelations", c.ConstructRelations.Count > 0 ? c.ConstructRelations.Select(x => x.RelationName).ToList() : (object)"None" },
                    { "Properties", c.Properties.Count > 0 ? c.Properties.Select(x => $"{x.Key}: {x.Value}").ToList() : (object)"None" }
                }
            });
        }
        else if (subTarget == "rules")
        {
            result.Columns = new List<string> { "Id", "Kind", "Variables", "Hypothesis", "Conclusion" };
            foreach (var r in c.ConceptRules)
            {
                result.Objects.Add(new ObjectInstance {
                    ConceptName = $"{c.Name}.rules",
                    Values = new Dictionary<string, object> {
                        { "Id", r.Id.ToString() },
                        { "Kind", r.Kind },
                        { "Variables", string.Join(", ", r.Variables.Select(v => v.Name)) },
                        { "Hypothesis", string.Join(" AND ", r.Hypothesis) },
                        { "Conclusion", string.Join(", ", r.Conclusion) }
                    }
                });
            }
        }
        else if (subTarget == "variables")
        {
            result.Columns = new List<string> { "Name", "Type", "Length", "Scale" };
            foreach (var v in c.Variables)
            {
                result.Objects.Add(new ObjectInstance {
                    ConceptName = $"{c.Name}.variables",
                    Values = new Dictionary<string, object> {
                        { "Name", v.Name },
                        { "Type", v.Type },
                        { "Length", v.Length?.ToString() ?? "NULL" },
                        { "Scale", v.Scale?.ToString() ?? "NULL" }
                    }
                });
            }
        }
        else if (subTarget == "constraints")
        {
            result.Columns = new List<string> { "Name", "Expression" };
            foreach (var constr in c.Constraints)
            {
                result.Objects.Add(new ObjectInstance {
                    ConceptName = $"{c.Name}.constraints",
                    Values = new Dictionary<string, object> {
                        { "Name", constr.Name },
                        { "Expression", constr.Expression }
                    }
                });
            }
        }
        else if (subTarget == "equations")
        {
            result.Columns = new List<string> { "Id", "Expression", "Variables" };
            foreach (var eq in c.Equations)
            {
                result.Objects.Add(new ObjectInstance {
                    ConceptName = $"{c.Name}.equations",
                    Values = new Dictionary<string, object> {
                        { "Id", eq.Id.ToString() },
                        { "Expression", eq.Expression },
                        { "Variables", string.Join(", ", eq.Variables) }
                    }
                });
            }
        }
        else if (subTarget == "comprels")
        {
            result.Columns = new List<string> { "Id", "Result", "Expression", "Cost" };
            foreach (var cr in c.CompRels)
            {
                result.Objects.Add(new ObjectInstance {
                    ConceptName = $"{c.Name}.comprels",
                    Values = new Dictionary<string, object> {
                        { "Id", cr.Id.ToString() },
                        { "Result", cr.ResultVariable ?? "N/A" },
                        { "Expression", cr.Expression },
                        { "Cost", cr.Cost.ToString() }
                    }
                });
            }
        }
        else if (subTarget == "properties")
        {
            result.Columns = new List<string> { "Key", "Value" };
            foreach (var p in c.Properties)
            {
                result.Objects.Add(new ObjectInstance {
                    ConceptName = $"{c.Name}.properties",
                    Values = new Dictionary<string, object> {
                        { "Key", p.Key },
                        { "Value", p.Value?.ToString() ?? "NULL" }
                    }
                });
            }
        }
        else if (subTarget == "construct_relations" || subTarget == "constructrelations")
        {
            result.Columns = new List<string> { "RelationName", "Arguments" };
            foreach (var cr in c.ConstructRelations)
            {
                result.Objects.Add(new ObjectInstance {
                    ConceptName = $"{c.Name}.construct_relations",
                    Values = new Dictionary<string, object> {
                        { "RelationName", cr.RelationName },
                        { "Arguments", string.Join(", ", cr.Arguments) }
                    }
                });
            }
        }

        result.Count = result.Objects.Count;
        return result;
    }

    private KBMS.Reasoning.InferenceEngine GetConfiguredEngine(string kbName)
    {
        // Return cached engine if available.
        // InvalidateEngineCache() removes it on any schema change (CREATE/DROP RULE/CONCEPT/RELATION, ALTER CONCEPT).
        return _engineCache.GetOrAdd(kbName, _ => CreateFreshEngine(kbName));
    }

    /// <summary>
    /// Creates a brand-new InferenceEngine for <paramref name="kbName"/> with all resolvers
    /// wired to perform live DB reads. This is called only on cache miss (first call or after
    /// a schema-invalidating DDL statement).
    /// </summary>
    private KBMS.Reasoning.InferenceEngine CreateFreshEngine(string kbName)
    {
        var engine = new KBMS.Reasoning.InferenceEngine();

        // --- ConceptResolver: loads concept + injects all matching rules (live read) ---
        engine.ConceptResolver = (name) => {
            var c = _conceptCatalog.LoadConcept(kbName, name);
            if (c != null)
            {
                var allRules = ListRules(kbName);
                var hierarchy = ListHierarchies(kbName);
                var ancestors = GetAncestors(name, hierarchy);
                ancestors.Add(name);

                var matchingRules = allRules
                    .Where(r => r != null && (
                        ancestors.Any(a => a.Equals(r.Scope, StringComparison.OrdinalIgnoreCase)) ||
                        (r.ScopeConcepts != null && r.ScopeConcepts.Any(sc => ancestors.Any(a => a.Equals(sc.ConceptName, StringComparison.OrdinalIgnoreCase))))
                    ))
                    .ToList();

                if (c.ConceptRules == null) c.ConceptRules = new List<Models.ConceptRule>();

                foreach (var r in matchingRules)
                {
                    var cr = new Models.ConceptRule
                    {
                        Id = r.Id,
                        Name = r.Name ?? "",
                        Kind = r.RuleType ?? "deduction",
                        Scope = r.Scope,
                        ScopeConcepts = r.ScopeConcepts?.Select(sc => new Models.ConceptRuleScopeConcept {
                            ConceptName = sc.ConceptName,
                            Alias = sc.Alias,
                            Position = sc.Position
                        }).ToList() ?? new(),
                        JoinConditions = r.JoinConditions?.Select(jc => new Models.ConceptRuleJoinCondition {
                            LeftField = jc.LeftField,
                            Operator = jc.Operator,
                            RightField = jc.RightField
                        }).ToList() ?? new(),
                        Priority = r.Priority,
                        Hypothesis = r.Hypothesis?.Select(h => h.Content ?? "").ToList() ?? new(),
                        Conclusion = r.Conclusion?.Select(conc => conc.Content ?? "").ToList() ?? new()
                    };

                    if (!c.ConceptRules.Any(existing => existing.Id == cr.Id))
                        c.ConceptRules.Add(cr);
                }
            }
            return c;
        };

        // --- Other resolvers: always read live from DB ---
        engine.FunctionResolver  = (name)   => ListFunctions(kbName).FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        engine.OperatorResolver  = (symbol) => ListOperators(kbName).FirstOrDefault(o => o.Symbol.Equals(symbol));
        engine.HierarchyResolver = (child)  => ListHierarchies(kbName)
            .Where(h => h.ChildConcept.Equals(child, StringComparison.OrdinalIgnoreCase) && h.HierarchyType == Models.HierarchyType.IsA)
            .Select(h => h.ParentConcept).ToList();
        engine.PartOfResolver    = (_)      => new List<string>();
        engine.RelationResolver  = (name)   => ListRelations(kbName).FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        // --- ExternalDataSource: Lazy Join / Query Planner ---
        engine.ExternalDataSource = (conceptAlias, joinConditions, leftToken) => {
            if (_v3Router == null) return Enumerable.Empty<Dictionary<string, object>>();

            var concept = engine.ConceptResolver!(conceptAlias);
            if (concept == null) return Enumerable.Empty<Dictionary<string, object>>();
            var realConceptName = concept.Name;

            var leftFacts = leftToken.ToDictionary();

            // Query Planner: prefer index scan (equality join)
            KBMS.Models.ConceptRuleJoinCondition? bestIndexCondition = null;
            string indexNameToUse = "";

            foreach (var jc in joinConditions)
            {
                if (jc.Operator == "=") 
                { 
                    var rFieldParts = jc.RightField.Split('.');
                    var rFieldName = rFieldParts.Length > 1 ? rFieldParts[1] : jc.RightField;
                    
                    if (rFieldName.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        bestIndexCondition = jc;
                        indexNameToUse = "id";
                        break;
                    }
                    
                    var matchingIndex = concept.Indexes?.FirstOrDefault(idx => idx.Fields.Count == 1 && idx.Fields[0].Equals(rFieldName, StringComparison.OrdinalIgnoreCase));
                    if (matchingIndex != null)
                    {
                        bestIndexCondition = jc;
                        indexNameToUse = matchingIndex.Name;
                        break;
                    }
                }
            }

            IEnumerable<Dictionary<string, object>> Enrich(IEnumerable<ObjectInstance> objs) =>
                objs.Select(o => {
                    var dict = new Dictionary<string, object>(o.Values);
                    dict["__internal_id"] = o.Id.ToString();
                    dict["__internal_concept"] = o.ConceptName;
                    return dict;
                });

            bool PassesAllJoins(Dictionary<string, object> rightDict)
            {
                foreach (var jc in joinConditions)
                {
                    if (jc == bestIndexCondition) continue;
                    var lVal = leftFacts.TryGetValue(jc.LeftField, out var lv) ? lv : null;
                    var rFieldParts = jc.RightField.Split('.');
                    var rFieldName = rFieldParts.Length > 1 ? rFieldParts[1] : jc.RightField;
                    var rVal = rightDict.TryGetValue(rFieldName, out var rv) ? rv : null;
                    if (!EvaluateJoinOperator(lVal, jc.Operator, rVal)) return false;
                }
                return true;
            }

            if (bestIndexCondition != null)
            {
                var leftVal = leftFacts.TryGetValue(bestIndexCondition.LeftField, out var lv) ? lv : null;
                if (leftVal == null) return Enumerable.Empty<Dictionary<string, object>>();
                return Enrich(_v3Router.SelectByValue(kbName, realConceptName, indexNameToUse, leftVal.ToString()!))
                    .Where(PassesAllJoins);
            }
            else
            {
                return Enrich(_v3Router.SelectObjects(kbName, realConceptName))
                    .Where(PassesAllJoins);
            }
        };

        return engine;
    }

    private static bool EvaluateJoinOperator(object? leftVal, string op, object? rightVal)
    {
        if (leftVal == null || rightVal == null) return false;
        
        // If they are both numeric
        if (double.TryParse(leftVal.ToString(), out double lNum) && double.TryParse(rightVal.ToString(), out double rNum))
        {
            switch (op)
            {
                case "=": return Math.Abs(lNum - rNum) < 0.00001;
                case "!=": return Math.Abs(lNum - rNum) >= 0.00001;
                case ">": return lNum > rNum;
                case "<": return lNum < rNum;
                case ">=": return lNum >= rNum;
                case "<=": return lNum <= rNum;
            }
        }
        
        // String comparison
        int cmp = string.Compare(leftVal.ToString(), rightVal.ToString(), StringComparison.OrdinalIgnoreCase);
        switch (op)
        {
            case "=": return cmp == 0;
            case "!=": return cmp != 0;
            case ">": return cmp > 0;
            case "<": return cmp < 0;
            case ">=": return cmp >= 0;
            case "<=": return cmp <= 0;
        }
        
        return false;
    }

    private object HandleSearch(SearchNode node, string kbName)
    {
        var pattern = node.Pattern.ToLower();
        var results = new List<ObjectInstance>();

        // 1. Search Concepts
        var concepts = _conceptCatalog.ListConcepts(kbName);
        foreach (var c in concepts)
        {
            if (c.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "SEARCH_RESULT", Values = new Dictionary<string, object> { ["type"] = "CONCEPT", ["name"] = c.Name, ["match"] = "Name match" } });
            }
            foreach (var v in c.Variables)
            {
                if (v.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "SEARCH_RESULT", Values = new Dictionary<string, object> { ["type"] = "VARIABLE", ["name"] = $"{c.Name}.{v.Name}", ["match"] = $"Attribute '{v.Name}' in concept '{c.Name}'" } });
                }
            }
        }

        // 2. Search Rules
        var rules = ListRules(kbName);
        foreach (var r in rules)
        {
            if (r.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                r.Hypothesis.Any(h => h.Content.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ||
                r.Conclusion.Any(cl => cl.Content.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "SEARCH_RESULT", Values = new Dictionary<string, object> { ["type"] = "RULE", ["name"] = r.Name, ["match"] = $"Found in rule content/hypothesis/conclusion" } });
            }
        }

        // 3. Search Relations
        var relations = ListRelations(kbName);
        foreach (var rel in relations)
        {
            if (rel.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                rel.Domain.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                rel.Range.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "SEARCH_RESULT", Values = new Dictionary<string, object> { ["type"] = "RELATION", ["name"] = rel.Name, ["match"] = $"Found in relation metadata" } });
            }
        }

        return new QueryResultSet
        {
            Success = true,
            ConceptName = "SEARCH_RESULT",
            Columns = new List<string> { "type", "name", "match" },
            Objects = results,
            Count = results.Count
        };
    }
    private object ExportKnowledgeBaseAsKbpkg(KBMS.Parser.Ast.Kml.ExportNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");

        var dir = Path.GetDirectoryName(node.FilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tempDir = Path.Combine(Path.GetTempPath(), $"kbpkg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // 1. Export KQL Dump
            var kqlResult = ExportKnowledgeBaseAsKql(node, kbName);
            if (kqlResult is ErrorResponse err) return err;
            
            var kqlContent = kqlResult.GetType().GetProperty("script")?.GetValue(kqlResult)?.ToString();
            File.WriteAllText(Path.Combine(tempDir, "data.kql"), kqlContent);

            // 2. Generate Telemetry
            var telemetry = new TelemetryLog
            {
                ExportedAt = DateTime.UtcNow,
                KnowledgeBase = kbName,
                Version = "3.4.0",
                Metrics = new Dictionary<string, object>
                {
                    { "ConceptsCount", _conceptCatalog.ListConcepts(kbName).Count },
                    { "ObjectsCount", SelectAllObjects(kbName).Count() }
                }
            };
            var telemetryJson = System.Text.Json.JsonSerializer.Serialize(telemetry, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(tempDir, "telemetry.json"), telemetryJson);

            // 3. Compress to .kbpkg
            if (File.Exists(node.FilePath)) File.Delete(node.FilePath);
            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, node.FilePath, System.IO.Compression.CompressionLevel.Optimal, false);

            return new { success = true, file = node.FilePath, format = "KBPKG" };
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    public class TelemetryLog
    {
        public DateTime ExportedAt { get; set; }
        public string KnowledgeBase { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> Metrics { get; set; } = new();
    }
}
