using System;
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
using KBMS.Storage.V3;
using KBMS.Knowledge.V3;

namespace KBMS.Knowledge;

/// <summary>
/// Knowledge Manager - Executes AST nodes against the storage engine
/// </summary>
public class KnowledgeManager
{
    private readonly StoragePool _storagePool;
    private readonly V3DataRouter _v3Router;
    public V3DataRouter V3Router => _v3Router;
    private readonly KBMS.Storage.V3.KbCatalog _kbCatalog;
    private readonly KBMS.Storage.V3.ConceptCatalog _conceptCatalog;
    private readonly KBMS.Storage.V3.UserCatalog _userCatalog;

    // Transaction buffering
    private bool _inTransaction = false;
    private readonly List<(string kbName, ObjectInstance obj)> _txBuffer = new();

    public KnowledgeManager(
        StoragePool storagePool,
        KbCatalog kbCatalog,
        ConceptCatalog conceptCatalog,
        UserCatalog userCatalog,
        V3DataRouter? v3Router = null)
    {
        _storagePool = storagePool;
        _kbCatalog = kbCatalog;
        _conceptCatalog = conceptCatalog;
        _userCatalog = userCatalog;
        _v3Router = v3Router ?? new V3DataRouter(storagePool);
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
        if (!CheckPrivilege(user, action, kbName))
        {
            return ErrorResponse.PermissionErrorResponse(action, kbName ?? "system");
        }

        // Execute the command
        return ExecuteQuery(ast, kbName);
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

    private bool CheckPrivilege(User user, string action, string? kbName)
    {
        // ROOT has all privileges
        if (user.Role == UserRole.ROOT)
            return true;

        return action switch
        {
            "CREATE" when kbName == null => user.SystemAdmin, // CREATE KNOWLEDGE BASE
            "DROP" when kbName == null => user.SystemAdmin,   // DROP KNOWLEDGE BASE
            "CREATE" => user.KbPrivileges.TryGetValue(kbName!, out var p1) && p1 == Privilege.ADMIN,
            "DROP" => user.KbPrivileges.TryGetValue(kbName!, out var p2) && p2 == Privilege.ADMIN,
            "SELECT" => user.KbPrivileges.ContainsKey(kbName!),
            "INSERT" => user.KbPrivileges.TryGetValue(kbName!, out var p3) && (p3 == Privilege.WRITE || p3 == Privilege.ADMIN),
            "INSERT_BULK" => user.KbPrivileges.TryGetValue(kbName!, out var pb) && (pb == Privilege.WRITE || pb == Privilege.ADMIN),
            "UPDATE" => user.KbPrivileges.TryGetValue(kbName!, out var p4) && (p4 == Privilege.WRITE || p4 == Privilege.ADMIN),
            "DELETE" => user.KbPrivileges.TryGetValue(kbName!, out var p5) && (p5 == Privilege.WRITE || p5 == Privilege.ADMIN),
            "GRANT" => user.SystemAdmin,
            "REVOKE" => user.SystemAdmin,
            "USE" => true,
            _ => false
        };
    }

    private object ExecuteQuery(AstNode ast, string? kbName)
    {
        return ast.Type switch
        {
            // DDL - Knowledge Base
            "CREATE_KNOWLEDGE_BASE" => HandleCreateKnowledgeBase((CreateKbNode)ast),
            "DROP_KNOWLEDGE_BASE" => HandleDropKnowledgeBase((DropKbNode)ast),
            "USE" => HandleUse((UseKbNode)ast),

            // DDL - Concept
            "CREATE_CONCEPT" => HandleCreateConcept((CreateConceptNode)ast, kbName!),
            "DROP_CONCEPT" => HandleDropConcept((DropConceptNode)ast, kbName!),
            "ALTER_CONCEPT" => HandleAlterConcept((AlterConceptNode)ast, kbName!),
            "CREATE_TRIGGER" => HandleCreateTrigger((KBMS.Parser.Ast.Kdl.CreateTriggerNode)ast, kbName!),

            // DCL - User
            "ALTER_USER" => HandleAlterUser((AlterUserNode)ast),
            "ALTER_KNOWLEDGE_BASE" => HandleAlterKnowledgeBase((AlterKbNode)ast),
            "CREATE_INDEX" => HandleCreateIndex((KBMS.Parser.Ast.Kdl.CreateIndexNode)ast, kbName!),
            "MAINTENANCE" => HandleMaintenance((KBMS.Parser.Ast.Kml.MaintenanceNode)ast, kbName!),
            "EXPLAIN" => HandleExplain((ExplainNode)ast, kbName),
            "DESCRIBE" => HandleDescribe((KBMS.Parser.Ast.Kql.DescribeNode)ast, kbName!),
            "EXPORT" => HandleExport((KBMS.Parser.Ast.Kml.ExportNode)ast, kbName!),
            "IMPORT" => HandleImport((KBMS.Parser.Ast.Kml.ImportNode)ast, kbName!),
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
            "INSERT" => HandleInsert((InsertNode)ast, kbName!),
            "INSERT_BULK" => HandleInsertBulk((InsertBulkNode)ast, kbName!),
            "UPDATE" => HandleUpdate((UpdateNode)ast, kbName!),
            "DELETE" => HandleDelete((DeleteNode)ast, kbName!),
            "SHOW_KNOWLEDGE_BASES" => HandleShowKnowledgeBases(),
            "SHOW_CONCEPTS" => HandleShowConcepts((ShowNode)ast, kbName!),
            "SHOW_CONCEPT" => HandleShowConcept((ShowNode)ast, kbName!),
            "SHOW_RULES" => HandleShowRules((ShowNode)ast, kbName!),
            "SHOW_RELATIONS" => HandleShowRelations((ShowNode)ast, kbName!),
            "SHOW_OPERATORS" => HandleShowOperators((ShowNode)ast, kbName!),
            "SHOW_FUNCTIONS" => HandleShowFunctions((ShowNode)ast, kbName!),
            "SHOW_HIERARCHIES" => HandleShowHierarchies((ShowNode)ast, kbName!),
            "SHOW_USERS" => HandleShowUsers(),
            "SHOW_PRIVILEGES_ON" => HandleShowPrivilegesOnKb((ShowNode)ast),
            "SHOW_PRIVILEGES_OF" => HandleShowPrivilegesOfUser((ShowNode)ast),

            // TCL - Transaction Control Language
            "BEGIN_TRANSACTION" => HandleBeginTransaction(),
            "COMMIT" => HandleCommit(kbName),
            "ROLLBACK" => HandleRollback(),

            _ => ErrorResponse.ExecutionErrorResponse($"Unknown command type: {ast.Type}")
        };
    }

    // ==================== TCL Handlers ====================

    private object HandleBeginTransaction()
    {
        _inTransaction = true;
        _txBuffer.Clear();
        return new { success = true, message = "V3 Transaction started (Buffered via WAL)." };
    }

    private object HandleCommit(string? kbName)
    {
        foreach (var (kb, obj) in _txBuffer)
        {
            _v3Router.InsertObject(kb, obj);
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

    private object HandleCreateTrigger(KBMS.Parser.Ast.Kdl.CreateTriggerNode node, string kbName)
    {
        var key = kbName;
        if (!_triggers.ContainsKey(key)) _triggers[key] = new();
        _triggers[key].Add(node);
        return new { success = true, message = $"Trigger '{node.TriggerName}' created on {node.Event} OF {node.TargetConcept} in KB '{kbName}'" };
    }

    // Called internally after INSERT/UPDATE/DELETE to fire matching triggers
    private void FireTriggers(string kbName, string conceptName, string eventType, Models.User executor)
    {
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

    private object HandleCreateKnowledgeBase(CreateKbNode node)
    {
        var kb = _kbCatalog.CreateKb(node.KbName, Guid.Empty, node.Description ?? "");
        if (kb == null)
            return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{node.KbName}' already exists.");

        return new { success = true, message = $"Knowledge base '{node.KbName}' created successfully (V3 Catalog)." };
    }

    private object HandleDropKnowledgeBase(DropKbNode node)
    {
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
        }
        return success
            ? new { success = true, message = $"Knowledge base '{node.KbName}' dropped successfully." }
            : ErrorResponse.ExecutionErrorResponse($"Knowledge base '{node.KbName}' not found.");
    }

    private object HandleUse(UseKbNode node)
    {
        var kb = _kbCatalog.LoadKb(node.KbName);
        return kb != null
            ? new { success = true, message = $"Now using knowledge base '{node.KbName}'.", currentKb = node.KbName }
            : ErrorResponse.ExecutionErrorResponse($"Knowledge base '{node.KbName}' not found.");
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
                        Scale = v.Scale
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
        return created
            ? new { success = true, message = $"Concept '{node.ConceptName}' created successfully (V3 Catalog)." }
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

    private object HandleDropConcept(DropConceptNode node, string kbName)
    {
        var success = _conceptCatalog.DropConcept(kbName, node.ConceptName);
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
        
        var hierarchy = new Hierarchy 
        { 
            ParentConcept = node.ParentConcept, 
            ChildConcept = node.ChildConcept, 
            HierarchyType = (Models.HierarchyType)node.HierarchyType 
        };
        kb.Hierarchies.Add(hierarchy);
        
        var success = _kbCatalog.SaveKbMetadata(kb);
        return success
            ? new { success = true, message = "Hierarchy added successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save hierarchy metadata.");
    }

    private object HandleRemoveHierarchy(RemoveHierarchyNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        
        var h = kb.Hierarchies.FirstOrDefault(x => x.ParentConcept == node.ParentConcept && x.ChildConcept == node.ChildConcept);
        if (h == null) return ErrorResponse.ExecutionErrorResponse("Hierarchy not found.");
        
        kb.Hierarchies.Remove(h);
        var success = _kbCatalog.SaveKbMetadata(kb);
        return success
            ? new { success = true, message = "Hierarchy removed successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to update hierarchy metadata.");
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
        return _kbCatalog.SaveKbMetadata(kb)
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
        return _kbCatalog.SaveKbMetadata(kb)
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
        var scope = node.ConceptName;
        // If scope is not explicitly provided (e.g. in standalone CREATE RULE), 
        // try to extract it from the first hypothesis: Student(grade > 90) -> Student
        if (string.IsNullOrEmpty(scope) && node.Hypothesis.Count > 0)
        {
            var firstHyp = GetExpressionString(node.Hypothesis[0]);
            var match = System.Text.RegularExpressions.Regex.Match(firstHyp, @"^(\w+)\(");
            if (match.Success) scope = match.Groups[1].Value;
        }

        var rule = new Rule
        {
            Id = Guid.NewGuid(),
            Name = node.RuleName,
            RuleType = node.RuleType.ToString().ToLower(),
            Scope = scope ?? "",
            Cost = node.Cost ?? 1,
            Hypothesis = node.Hypothesis.Select(h => ToModelExpression(h)).ToList(),
            Conclusion = node.Conclusions.Select(c => ToModelExpression(c)).ToList()
        };

        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        kb.Rules.Add(rule);
        return _kbCatalog.SaveKbMetadata(kb)
            ? new { success = true, message = $"Rule '{node.RuleName}' created successfully (V3 Engine)." }
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
        return ast.ToString();
    }



    private object HandleDropRule(DropRuleNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return ErrorResponse.ExecutionErrorResponse("KB not found.");
        var rule = kb.Rules.FirstOrDefault(r => r.Name.Equals(node.RuleName, StringComparison.OrdinalIgnoreCase));
        if (rule == null) return ErrorResponse.ExecutionErrorResponse("Rule not found.");
        kb.Rules.Remove(rule);
        return _kbCatalog.SaveKbMetadata(kb)
            ? new { success = true, message = $"Rule '{node.RuleName}' dropped successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to save KB metadata.");
    }

    private object HandleCreateUser(CreateUserNode node)
    {
        var role = Enum.TryParse<UserRole>(node.Role, out var r) ? r : UserRole.USER;
        var user = _userCatalog.CreateUser(node.Username, node.Password, role);
        return user != null
            ? new { success = true, message = $"User '{node.Username}' created successfully (V3 Catalog)." }
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
            ? new { success = true, message = $"Privilege {node.Privilege} on {node.KbName} granted to {node.Username} (V3 Catalog)." }
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

    private object HandleSelect(SelectNode node, string kbName)
    {
        try
        {
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
                    conceptMetadata = _conceptCatalog.ListConcepts(kbName).FirstOrDefault(c => c.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
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
                    Values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["child_concept"]   = h.ChildConcept,
                        ["hierarchy_type"]  = h.HierarchyType == KBMS.Models.HierarchyType.IsA ? "IS_A" : "PART_OF",
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
                    Values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Name"]       = r.Name,
                        ["Type"]       = r.RuleType.ToString(),
                        ["Scope"]      = r.Scope,
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
                    // ✅ V3 Route: Uses the Optimizer and Execution Pipeline (Pushdown + Joins)
                    objects = _v3Router.ExecuteSelect(kbName, node, conceptMetadata);
                }
                
                // Merge transaction buffer (shadow visibility)
                if (_inTransaction)
                {
                    objects.AddRange(_txBuffer
                        .Where(t => t.kbName == kbName && t.obj.ConceptName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
                        .Select(t => t.obj));
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
                        qrs_data.Columns = qrs_data.Objects[0].Values.Keys.ToList();
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
                var colsToInclude = node.SelectColumns.Where(sc => !sc.IsStar).ToList();
                if (colsToInclude.Count > 0)
                {
                    var tableAlias = node.Alias ?? entityName;
                    var engine = GetConfiguredEngine(kbName);

                    objects = objects.Select(obj =>
                    {
                        var newValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        
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
                                    newValues[outName] = null;
                                    if (!string.IsNullOrEmpty(targetVar))
                                    {
                                        var resolvedConcept = engine.ConceptResolver?.Invoke(entityName) ?? conceptMetadata;
                                        if (resolvedConcept != null)
                                        {
                                            var solveResult = engine.FindClosure(resolvedConcept, evalParams, new List<string> { targetVar });
                                            if (solveResult.Success && solveResult.DerivedFacts.ContainsKey(targetVar))
                                                newValues[outName] = solveResult.DerivedFacts[targetVar];
                                            else if (!solveResult.Success && !string.IsNullOrEmpty(solveResult.ErrorMessage))
                                                newValues[outName] = $"[ERROR] {solveResult.ErrorMessage}";
                                        }
                                    }
                                }
                                else
                                {
                                    // 2. Try evaluating as NCalc expression (for math/aggregate functions)
                                    try {
                                        var exprForEval = col.Expression != null ? col.Expression.ToString() : sourceName;
                                        newValues[outName] = engine.EvaluateFormula(exprForEval, evalParams);
                                    } catch {
                                        newValues[outName] = null;
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
        var value = ResolveValue(obj, condition.Field, a, c);
        if (value == null) return false;

        var compareValue = condition.Value;

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
        var joinObjects = _v3Router.SelectObjects(kbName, join.Target, concept: targetConcept);

        var result = new List<ObjectInstance>();

        foreach (var obj in objects)
        {
            foreach (var joinObj in joinObjects)
            {
                if (join.OnCondition != null)
                {
                    if (EvaluateJoinCondition(obj, leftAlias, leftConcept, joinObj, join.Alias, join.Target, join.OnCondition))
                    {
                        var merged = MergeObjects(obj, joinObj, join.Alias);
                        result.Add(merged);
                    }
                }
                else
                {
                    // No ON condition - cross join
                    var merged = MergeObjects(obj, joinObj, join.Alias);
                    result.Add(merged);
                }
            }
        }

        return result;
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
                row[gb] = firstObj.Values.GetValueOrDefault(gb);
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

    private object HandleInsert(InsertNode node, string kbName)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null)
        {
            return ErrorResponse.ExecutionErrorResponse($"Knowledge base '{kbName}' not found.");
        }

        // Load concept to validate it exists and get variable names for positional values
        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        if (concept == null)
        {
            return ErrorResponse.ExecutionErrorResponse($"Concept '{node.ConceptName}' does not exist.");
        }

        var values = new Dictionary<string, object>();

        // Check if values use positional syntax (keys like _0, _1, etc.)
        var positionalValues = node.Values
            .Where(kv => kv.Key.StartsWith("_"))
            .OrderBy(kv => int.Parse(kv.Key.Substring(1)))
            .Select(kv => kv.Value)
            .ToList();

        var namedValues = node.Values
            .Where(kv => !kv.Key.StartsWith("_"))
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

        var obj = new ObjectInstance
        {
            Id = Guid.NewGuid(),
            KbId = kb.Id,
            ConceptName = node.ConceptName,
            Values = values
        };

        // V3 engine write (buffered if in transaction)
        if (_inTransaction)
        {
            _txBuffer.Add((kbName, obj));
            return new { success = true, message = $"Object inserted successfully with ID: {obj.Id}", data = obj.Values };
        }

        var success = _v3Router.InsertObject(kbName, obj);
        return success
            ? new { success = true, message = $"Object inserted successfully with ID: {obj.Id}", data = obj.Values }
            : ErrorResponse.ExecutionErrorResponse("Failed to insert object.");
    }

    private object HandleInsertBulk(InsertBulkNode node, string kbName)
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

        foreach (var rowValues in node.Rows)
        {
            var values = new Dictionary<string, object>();

            var positionalValues = rowValues
                .Where(kv => kv.Key.StartsWith("_"))
                .OrderBy(kv => int.Parse(kv.Key.Substring(1)))
                .Select(kv => kv.Value)
                .ToList();

            var namedValues = rowValues
                .Where(kv => !kv.Key.StartsWith("_"))
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

            bool ok = _v3Router.InsertObject(kbName, obj);
            if (ok) inserted++;
            else { failed++; errors.Add($"Row {inserted + failed + 1}: failed"); }
        }

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
        }
        catch
        {
            // Fallback to raw if conversion fails
        }

        return rawValue;
    }

    private object HandleUpdate(UpdateNode node, string kbName)
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
                    var formula = kv.Value.ToString();
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

            if (_v3Router.UpdateObject(kbName, node.ConceptName, obj.Id, obj.Values, concept))
            {
                updatedCount++;
            }
            else
            {
                success = false;
            }
        }

        return success
            ? new { success = true, message = $"{updatedCount} object(s) updated successfully (V3 Engine)." }
            : ErrorResponse.ExecutionErrorResponse("Failed to update some object(s).");
    }

    private object HandleDelete(DeleteNode node, string kbName)
    {
        // Optimized V3 delete: push conditions down
        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        int deletedCount = _v3Router.DeleteObjects(kbName, node.ConceptName, values => EvaluatePredicate(values, node.Conditions, kbName, null, node.ConceptName), concept);

        if (deletedCount == 0)
        {
            return ErrorResponse.ExecutionErrorResponse("No objects found matching conditions.");
        }

        return new { success = true, message = $"{deletedCount} object(s) deleted successfully (V3 Engine)." };
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
        var concepts = _conceptCatalog.ListConcepts(kbName);
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
            TargetName = node.ConceptName 
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

                migratedObjects.Add((obj.Id, newValues));
            }

            // If we get here, all data is valid. Commit the concept and update objects.
            _conceptCatalog.UpdateConcept(kbName, concept);
            foreach (var migration in migratedObjects)
            {
                _v3Router.UpdateObject(kbName, cName, migration.Id, migration.Values, concept);
            }
            
            Console.WriteLine($"[V3] Persisted altered concept '{cName}' and migrated/validated {existingObjects.Count} objects.");
        }

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
        return new { success = true, message = $"Index '{node.IndexName}' creation handled by V3 auto-indexer (Placeholder)." };
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
                var valuesDict = new Dictionary<string, object>
                {
                    { "Concept", c.Name },
                    { "Aliases", c.Aliases.Count > 0 ? string.Join(", ", c.Aliases) : "None" },
                    { "BaseObjects", c.BaseObjects.Count > 0 ? string.Join(", ", c.BaseObjects) : "None" },
                    { "Variables", c.Variables.Count > 0 ? string.Join("\n", c.Variables.Select(v => $"{v.Name} ({GetFormattedType(v)})")) : "None" },
                    { "SameVariables", c.SameVariables.Count > 0 ? string.Join("\n", c.SameVariables.Select(sv => $"{sv.Variable1} = {sv.Variable2}")) : "None" },
                    { "Constraints", c.Constraints.Count > 0 ? string.Join("\n", c.Constraints.Select(ct => ct.Expression)) : "None" },
                    { "Equations", c.Equations.Count > 0 ? string.Join("\n", c.Equations.Select(eq => eq.Expression)) : "None" },
                    { "Rules", c.ConceptRules.Count > 0 ? string.Join("\n", c.ConceptRules.Select(r => $"{(string.IsNullOrEmpty(r.Kind) ? "RULE" : r.Kind)}: {string.Join(" AND ", r.Hypothesis)} => {string.Join(", ", r.Conclusion)}")) : "None" },
                    { "CompRels", c.CompRels.Count > 0 ? string.Join("\n", c.CompRels.Select(cr => $"[Rank {cr.Rank}] {string.Join(",", cr.InputVariables)} -> {cr.ResultVariable} (Cost:{cr.Cost}) Expr: {cr.Expression}")) : "None" },
                    { "ConstructRelations", c.ConstructRelations.Count > 0 ? string.Join("\n", c.ConstructRelations.Select(cr => $"{cr.RelationName}({string.Join(", ", cr.Arguments)})")) : "None" },
                    { "Properties", c.Properties.Count > 0 ? string.Join("\n", c.Properties.Select(p => $"{p.Key}: {p.Value}")) : "None" }
                };

                qrs.Objects.Add(new ObjectInstance { Values = valuesDict });
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

    private object HandleImport(KBMS.Parser.Ast.Kml.ImportNode node, string kbName)
    {
        try
        {
            if (!File.Exists(node.FilePath))
                return ErrorResponse.ExecutionErrorResponse($"File not found: {node.FilePath}");

            var json = File.ReadAllText(node.FilePath);
            var imported = System.Text.Json.JsonSerializer.Deserialize<List<KBMS.Models.ObjectInstance>>(json);
            if (imported == null) return ErrorResponse.ExecutionErrorResponse("Failed to deserialize import file");

            int count = 0;
            foreach (var obj in imported)
            {
                if (node.TargetName != "*" && !obj.ConceptName.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase))
                    continue;
                obj.Id = Guid.NewGuid(); // Assign new ID to avoid collisions
                _v3Router.InsertObject(kbName, obj);
                count++;
            }

            return new { success = true, imported = count, concept = node.TargetName, file = node.FilePath };
        }
        catch (Exception ex)
        {
            return ErrorResponse.ExecutionErrorResponse(ex.Message);
        }
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
        var engine = new KBMS.Reasoning.InferenceEngine();
        var allRules = ListRules(kbName);

        engine.ConceptResolver = (name) => {
            var c = _conceptCatalog.LoadConcept(kbName, name);
            if (c != null) {
                var hierarchy = ListHierarchies(kbName);
                var ancestors = GetAncestors(name, hierarchy);
                ancestors.Add(name);

                var matchingRules = allRules
                    .Where(r => r != null && ancestors.Any(a => a.Equals(r.Scope, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                c.ConceptRules = matchingRules
                    .Select(r => {
                        var scopeName = r.Scope;
                        return new Models.ConceptRule { 
                            Id = r.Id, 
                            Kind = r.Name ?? r.RuleType ?? "deduction", 
                            Hypothesis = r.Hypothesis?.Select(h => {
                                var s = h.Content ?? "";
                                if (!string.IsNullOrEmpty(scopeName) && s.StartsWith(scopeName + "(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
                                    return s.Substring(scopeName.Length + 1, s.Length - scopeName.Length - 2);
                                return s;
                            }).ToList() ?? new(), 
                            Conclusion = r.Conclusion?.Select(conc => {
                                var s = conc.Content ?? "";
                                if (!string.IsNullOrEmpty(scopeName) && s.StartsWith(scopeName + "(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
                                    return s.Substring(scopeName.Length + 1, s.Length - scopeName.Length - 2);
                                return s;
                            }).ToList() ?? new()
                        };
                    }).ToList();
            }
            return c;
        };

        var functions = ListFunctions(kbName);
        engine.FunctionResolver = (name) => functions.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        var operators = ListOperators(kbName);
        engine.OperatorResolver = (symbol) => operators.FirstOrDefault(o => o.Symbol.Equals(symbol));

        var hierarchies = ListHierarchies(kbName);
        engine.HierarchyResolver = (childName) => hierarchies
            .Where(h => h.ChildConcept.Equals(childName, StringComparison.OrdinalIgnoreCase) && h.HierarchyType == Models.HierarchyType.IsA)
            .Select(h => h.ParentConcept)
            .ToList();

        engine.PartOfResolver = (parentName) => hierarchies
            .Where(h => h.ParentConcept.Equals(parentName, StringComparison.OrdinalIgnoreCase) && h.HierarchyType == Models.HierarchyType.PartOf)
            .Select(h => h.ChildConcept)
            .ToList();

        var relations = ListRelations(kbName);
        engine.RelationResolver = (name) => relations.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        return engine;
    }
}
