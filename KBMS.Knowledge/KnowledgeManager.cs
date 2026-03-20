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

namespace KBMS.Knowledge;

/// <summary>
/// Knowledge Manager - Executes AST nodes against the storage engine
/// </summary>
public class KnowledgeManager
{
    private readonly StorageEngine _storage;

    public KnowledgeManager(StorageEngine storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Execute an AST node with user context
    /// </summary>
    public object Execute(AstNode ast, User user, string? currentKb)
    {
        if (ast == null)
        {
            return new { error = "Query is empty, a comment, or could not be parsed." };
        }

        // Determine KB name
        var kbName = DetermineKbName(ast) ?? currentKb;

        // Check if KB is required
        if (RequiresKb(ast) && kbName == null)
        {
            return new { error = "No knowledge base selected. Use 'USE <kbname>' first." };
        }

        // Check privileges
        var action = DetermineAction(ast);
        if (!CheckPrivilege(user, action, kbName))
        {
            return new { error = $"Permission denied: {action} on {kbName ?? "system"}" };
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
            "UPDATE" => HandleUpdate((UpdateNode)ast, kbName!),
            "DELETE" => HandleDelete((DeleteNode)ast, kbName!),
            "SOLVE" => HandleSolve((SolveNode)ast, kbName!),
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

            _ => new { error = $"Unknown command type: {ast.Type}" }
        };
    }

    // ==================== TCL Handlers ====================

    private object HandleBeginTransaction()
    {
        if (_storage.IsTransactionActive())
            return new { error = "A transaction is already active. COMMIT or ROLLBACK first." };

        _storage.BeginTransaction();
        return new { success = true, message = "Transaction started. Changes are buffered in RAM until COMMIT." };
    }

    private object HandleCommit(string? kbName)
    {
        if (!_storage.IsTransactionActive())
            return new { error = "No active transaction. Use BEGIN TRANSACTION first." };

        _storage.CommitTransaction(kbName ?? string.Empty);
        return new { success = true, message = "Transaction committed. All changes flushed to disk." };
    }

    private object HandleRollback()
    {
        if (!_storage.IsTransactionActive())
            return new { error = "No active transaction. Use BEGIN TRANSACTION first." };

        _storage.RollbackTransaction();
        return new { success = true, message = "Transaction rolled back. All uncommitted changes discarded." };
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
        var kb = _storage.CreateKb(node.KbName, Guid.Empty, node.Description ?? "");
        return new { success = true, message = $"Knowledge base '{kb.Name}' created successfully." };
    }

    private object HandleDropKnowledgeBase(DropKbNode node)
    {
        var success = _storage.DropKb(node.KbName);
        return success
            ? new { success = true, message = $"Knowledge base '{node.KbName}' dropped successfully." }
            : new { error = $"Knowledge base '{node.KbName}' not found." };
    }

    private object HandleUse(UseKbNode node)
    {
        var kb = _storage.LoadKb(node.KbName);
        return kb != null
            ? new { success = true, message = $"Now using knowledge base '{node.KbName}'.", currentKb = node.KbName }
            : new { error = $"Knowledge base '{node.KbName}' not found." };
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
                // Primitive type — keep as-is
                expandedVariables.Add(new Variable
                {
                    Name = v.Name,
                    Type = v.Type,
                    Length = v.Length,
                    Scale = v.Scale
                });
            }
            else
            {
                // Possibly a Concept type — look it up
                var referencedConcept = _storage.LoadConcept(kbName, v.Type);

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

        var created = _storage.CreateConcept(kbName, concept);
        return created != null
            ? new { success = true, message = $"Concept '{node.ConceptName}' created successfully." }
            : new { error = $"Concept '{node.ConceptName}' already exists." };
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
        var success = _storage.DropConcept(kbName, node.ConceptName);
        return success
            ? new { success = true, message = $"Concept '{node.ConceptName}' dropped successfully." }
            : new { error = $"Concept '{node.ConceptName}' not found or is in use." };
    }

    private object HandleAddVariable(AddVariableNode node, string kbName)
    {
        var success = _storage.AddVariableToConcept(kbName, node.ConceptName, node.VariableName, node.VariableType, node.Length, node.Scale);
        return success
            ? new { success = true, message = $"Variable '{node.VariableName}' added to concept '{node.ConceptName}'." }
            : new { error = "Failed to add variable." };
    }

    private object HandleAddHierarchy(AddHierarchyNode node, string kbName)
    {
        var hierarchy = _storage.AddHierarchy(kbName, node.ParentConcept, node.ChildConcept, (Models.HierarchyType)node.HierarchyType);
        return hierarchy != null
            ? new { success = true, message = "Hierarchy added successfully." }
            : new { error = "Hierarchy already exists or failed to add." };
    }

    private object HandleRemoveHierarchy(RemoveHierarchyNode node, string kbName)
    {
        var success = _storage.RemoveHierarchy(kbName, node.ParentConcept, node.ChildConcept, (Models.HierarchyType)node.HierarchyType);
        return success
            ? new { success = true, message = "Hierarchy removed successfully." }
            : new { error = "Hierarchy not found." };
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

        var created = _storage.CreateRelation(kbName, relation);
        return created != null
            ? new { success = true, message = $"Relation '{node.RelationName}' created successfully." }
            : new { error = $"Relation '{node.RelationName}' already exists." };
    }

    private object HandleDropRelation(DropRelationNode node, string kbName)
    {
        var success = _storage.DropRelation(kbName, node.RelationName);
        return success
            ? new { success = true, message = $"Relation '{node.RelationName}' dropped successfully." }
            : new { error = $"Relation '{node.RelationName}' not found." };
    }

    private object HandleCreateOperator(CreateOperatorNode node, string kbName)
    {
        Console.WriteLine($"(DEBUG) Creating Operator '{node.Symbol}' with Body: '{node.Body}'");
        var op = new Operator
        {
            Symbol = node.Symbol,
            ParamTypes = node.ParamTypes,
            ReturnType = node.ReturnType,
            Body = node.Body,
            Properties = node.Properties
        };

        var created = _storage.CreateOperator(kbName, op);

        return created != null
            ? new { success = true, message = $"Operator '{node.Symbol}' created successfully." }
            : new { error = $"Operator '{node.Symbol}' already exists." };
    }

    private object HandleDropOperator(DropOperatorNode node, string kbName)
    {
        var success = _storage.DropOperator(kbName, node.Symbol);
        return success
            ? new { success = true, message = $"Operator '{node.Symbol}' dropped successfully." }
            : new { error = $"Operator '{node.Symbol}' not found." };
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

        var created = _storage.CreateFunction(kbName, func);

        return created != null
            ? new { success = true, message = $"Function '{node.FunctionName}' created successfully." }
            : new { error = $"Function '{node.FunctionName}' already exists." };
    }

    private object HandleDropFunction(DropFunctionNode node, string kbName)
    {
        var success = _storage.DropFunction(kbName, node.FunctionName);
        return success
            ? new { success = true, message = $"Function '{node.FunctionName}' dropped successfully." }
            : new { error = $"Function '{node.FunctionName}' not found." };
    }

    private object HandleAddComputation(AddComputationNode node, string kbName)
    {
        var success = _storage.AddComputation(kbName, node.ConceptName, node.InputVariables, node.ResultVariable, node.Formula, node.Cost ?? 1);
        return success
            ? new { success = true, message = "Computation added successfully." }
            : new { error = "Computation already exists or failed to add." };
    }

    private object HandleRemoveComputation(RemoveComputationNode node, string kbName)
    {
        var success = _storage.RemoveComputation(kbName, node.ConceptName, node.VariableName);
        return success
            ? new { success = true, message = "Computation removed successfully." }
            : new { error = "Computation not found." };
    }

    private object HandleCreateRule(CreateRuleNode node, string kbName)
    {
        var rule = new Rule
        {
            Name = node.RuleName,
            RuleType = node.RuleType.ToString().ToLower(),
            Scope = node.ConceptName,
            Cost = node.Cost ?? 1,
            Hypothesis = node.Hypothesis.Select(h => new Expression { Type = "expression", Content = h }).ToList(),
            Conclusion = node.Conclusions.Select(c => new Expression { Type = "expression", Content = c }).ToList()
        };

        var created = _storage.CreateRule(kbName, rule);
        return created != null
            ? new { success = true, message = $"Rule '{node.RuleName}' created successfully." }
            : new { error = $"Rule '{node.RuleName}' already exists." };
    }

    private object HandleDropRule(DropRuleNode node, string kbName)
    {
        var success = _storage.DropRule(kbName, node.RuleName);
        return success
            ? new { success = true, message = $"Rule '{node.RuleName}' dropped successfully." }
            : new { error = $"Rule '{node.RuleName}' not found." };
    }

    private object HandleCreateUser(CreateUserNode node)
    {
        var role = Enum.TryParse<UserRole>(node.Role, out var r) ? r : UserRole.USER;
        var user = _storage.CreateUser(node.Username, node.Password, role, node.SystemAdmin);
        return user != null
            ? new { success = true, message = $"User '{node.Username}' created successfully." }
            : new { error = $"User '{node.Username}' already exists." };
    }

    private object HandleDropUser(DropUserNode node)
    {
        var success = _storage.DropUser(node.Username);
        return success
            ? new { success = true, message = $"User '{node.Username}' dropped successfully." }
            : new { error = $"User '{node.Username}' not found." };
    }

    private object HandleGrant(GrantNode node)
    {
        var success = _storage.GrantPrivilege(node.KbName, node.Username, node.Privilege);
        return success
            ? new { success = true, message = $"Privilege {node.Privilege} on {node.KbName} granted to {node.Username}." }
            : new { error = "Failed to grant privilege." };
    }

    private object HandleRevoke(RevokeNode node)
    {
        var success = _storage.RevokePrivilege(node.KbName, node.Username);
        return success
            ? new { success = true, message = $"Privilege on {node.KbName} revoked from {node.Username}." }
            : new { error = "Failed to revoke privilege." };
    }

    // ==================== DML Handlers ====================

    private object HandleSelect(SelectNode node, string kbName)
    {
        try
        {
            // 1. Load objects by concept
            var objects = _storage.SelectObjects(kbName, null);
            objects = objects.Where(o => o.ConceptName == node.ConceptName).ToList();

            // 2. Apply WHERE conditions
            if (node.Conditions.Count > 0)
            {
                objects = EvaluateConditions(objects, node.Conditions);
            }

            // 3. Apply JOINs
            foreach (var join in node.Joins)
            {
                objects = ApplyJoin(objects, join, kbName);
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

            // Return error if no results found
            if (objects.Count == 0)
            {
                return new { error = $"No objects found for concept '{node.ConceptName}'" };
            }

            return new
            {
                success = true,
                conceptName = node.ConceptName,
                count = objects.Count,
                objects
            };
        }
        catch (Exception ex)
        {
            return new { error = $"SELECT failed: {ex.Message}" };
        }
    }

    private List<ObjectInstance> EvaluateConditions(List<ObjectInstance> objects, List<Condition> conditions)
    {
        var result = new List<ObjectInstance>();

        foreach (var obj in objects)
        {
            if (EvaluateObjectConditions(obj, conditions))
            {
                result.Add(obj);
            }
        }

        return result;
    }

    private bool EvaluateObjectConditions(ObjectInstance obj, List<Condition> conditions)
    {
        if (conditions.Count == 0) return true;

        var result = EvaluateCondition(obj, conditions[0]);

        for (int i = 1; i < conditions.Count; i++)
        {
            var cond = conditions[i];
            var value = EvaluateCondition(obj, cond);

            if (conditions[i - 1].LogicalOperator == "OR")
            {
                result = result || value;
            }
            else // AND (default)
            {
                result = result && value;
            }
        }

        return result;
    }

    private bool EvaluateCondition(ObjectInstance obj, Condition condition)
    {
        if (!obj.Values.TryGetValue(condition.Field, out var value))
            return false;

        var compareValue = condition.Value;

        return condition.Operator switch
        {
            "=" => Equals(value, compareValue) || CompareValues(value, compareValue) == 0,
            "<>" or "!=" => !Equals(value, compareValue) && CompareValues(value, compareValue) != 0,
            ">" => CompareValues(value, compareValue) > 0,
            "<" => CompareValues(value, compareValue) < 0,
            ">=" => CompareValues(value, compareValue) >= 0,
            "<=" => CompareValues(value, compareValue) <= 0,
            _ => false
        };
    }

    private int CompareValues(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // Try numeric comparison
        if (TryConvertToDouble(a, out var da) && TryConvertToDouble(b, out var db))
        {
            return da.CompareTo(db);
        }

        // String comparison
        return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
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

    private List<ObjectInstance> ApplyJoin(List<ObjectInstance> objects, JoinClause join, string kbName)
    {
        var joinObjects = _storage.SelectObjects(kbName, null);
        joinObjects = joinObjects.Where(o => o.ConceptName == join.Target).ToList();

        var result = new List<ObjectInstance>();

        foreach (var obj in objects)
        {
            foreach (var joinObj in joinObjects)
            {
                if (join.OnCondition != null)
                {
                    if (EvaluateJoinCondition(obj, joinObj, join.OnCondition))
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

    private bool EvaluateJoinCondition(ObjectInstance left, ObjectInstance right, Condition condition)
    {
        var leftValue = left.Values.GetValueOrDefault(condition.Field);
        var rightValue = right.Values.GetValueOrDefault(condition.Value?.ToString() ?? "");

        if (leftValue == null || rightValue == null) return false;

        return condition.Operator switch
        {
            "=" => Equals(leftValue, rightValue),
            _ => false
        };
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

        return new { success = true, count = result.Count, groups = result };
    }

    private object EvaluateAggregates(List<ObjectInstance> objects, List<AggregateClause> aggregates)
    {
        var result = new Dictionary<string, object>();

        foreach (var agg in aggregates)
        {
            var value = EvaluateAggregate(objects, agg);
            result[agg.Alias ?? agg.AggregateType] = value;
        }

        return new { success = true, aggregates = result };
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
        var kb = _storage.LoadKb(kbName);
        if (kb == null)
        {
            return new { error = $"Knowledge base '{kbName}' not found." };
        }

        // Load concept to validate it exists and get variable names for positional values
        var concept = _storage.LoadConcept(kbName, node.ConceptName);
        if (concept == null)
        {
            return new { error = $"Concept '{node.ConceptName}' does not exist." };
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
                var varName = concept.Variables[i].Name;
                values[varName] = ConvertValueNodeToObject(positionalValues[i]);
            }
        }

        // Add named values (these override positional values if there's a conflict)
        foreach (var kv in namedValues)
        {
            values[kv.Key] = ConvertValueNodeToObject(kv.Value);
        }

        var obj = new ObjectInstance
        {
            Id = Guid.NewGuid(),
            KbId = kb.Id,
            ConceptName = node.ConceptName,
            Values = values
        };

        var success = _storage.InsertObject(kbName, obj);
        return success
            ? new { success = true, message = $"Object inserted successfully with ID: {obj.Id}" }
            : new { error = "Failed to insert object." };
    }

    private object ConvertValueNodeToObject(ValueNode valueNode)
    {
        return valueNode.ValueType switch
        {
            "number" => TryConvertToDouble(valueNode.Value, out var d) ? d : 0,
            "string" => valueNode.Value?.ToString() ?? "",
            "boolean" => valueNode.Value is bool b ? b : valueNode.Value?.ToString()?.ToLower() == "true",
            "identifier" => valueNode.Value?.ToString() ?? "",
            _ => valueNode.Value ?? ""
        };
    }

    private object HandleUpdate(UpdateNode node, string kbName)
    {
        var conditions = ConvertConditions(node.Conditions);
        var objects = _storage.SelectObjects(kbName, conditions.Count > 0 ? conditions : null);

        if (objects.Count == 0)
        {
            return new { error = "No objects found matching conditions." };
        }

        var values = new Dictionary<string, object>();
        foreach (var kv in node.SetValues)
        {
            values[kv.Key] = ConvertExpressionToValue(kv.Value);
        }

        var success = true;
        foreach (var obj in objects)
        {
            success &= _storage.UpdateObject(kbName, obj.Id, values);
        }

        return success
            ? new { success = true, message = $"{objects.Count} object(s) updated successfully." }
            : new { error = "Failed to update object(s)." };
    }

    private object HandleDelete(DeleteNode node, string kbName)
    {
        var conditions = ConvertConditions(node.Conditions);
        var objects = _storage.SelectObjects(kbName, conditions.Count > 0 ? conditions : null).ToList();

        if (objects.Count == 0)
        {
            return new { error = "No objects found matching conditions." };
        }

        var success = true;
        foreach (var obj in objects)
        {
            success &= _storage.DeleteObject(kbName, obj.Id);
        }

        return success
            ? new { success = true, message = $"{objects.Count} object(s) deleted successfully." }
            : new { error = "Failed to delete object(s)." };
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

    private object HandleSolve(SolveNode node, string kbName)
    {
        var kb = _storage.LoadKb(kbName);
        if (kb == null)
            return new { error = $"Knowledge base '{kbName}' not found." };

        var concept = _storage.LoadConcept(kbName, node.ConceptName);
        if (concept == null)
            return new { error = $"Concept '{node.ConceptName}' does not exist." };

        // Convert GivenFacts strings to objects where possible (best effort mapping)
        var initialFacts = new Dictionary<string, object>();
        foreach (var kvp in node.GivenFacts)
        {
            if (double.TryParse(kvp.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
                initialFacts[kvp.Key] = d;
            else if (bool.TryParse(kvp.Value, out var b))
                initialFacts[kvp.Key] = b;
            else
                initialFacts[kvp.Key] = kvp.Value;
        }

        // Initialize engine and solve
        var engine = new KBMS.Reasoning.InferenceEngine();
        
        // (RC8) Pre-load rules to attach to concepts during resolution
        var allRules = _storage.ListRules(kbName);

        engine.ConceptResolver = (name) => {
            var c = _storage.LoadConcept(kbName, name);
            if (c != null) {
                // (RC8) Logic: Rules scoped to this concept OR any parent concept (Inference)
                var hierarchy = _storage.ListHierarchies(kbName);
                var ancestors = GetAncestors(name, hierarchy);
                ancestors.Add(name);

                c.ConceptRules = allRules
                    .Where(r => r != null && ancestors.Any(a => a.Equals(r.Scope, StringComparison.OrdinalIgnoreCase)))
                    .Select(r => new Models.ConceptRule { 
                        Id = r.Id, 
                        Kind = r.RuleType ?? "deduction", 
                        Hypothesis = r.Hypothesis?.Select(h => h.Content).Where(content => content != null).ToList() ?? new(), 
                        Conclusion = r.Conclusion?.Select(conc => conc.Content).Where(content => content != null).ToList() ?? new()
                    }).ToList();
            }
            return c;
        };
        
        // (RC7) Provide Function and Operator resolvers
        var functions = _storage.ListFunctions(kbName);
        engine.FunctionResolver = (name) => functions.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        var operators = _storage.ListOperators(kbName);
        engine.OperatorResolver = (symbol) => operators.FirstOrDefault(o => o.Symbol.Equals(symbol));

        // (RC8) Provide Hierarchy resolver
        var hierarchies = _storage.ListHierarchies(kbName);
        engine.HierarchyResolver = (childName) => hierarchies
            .Where(h => h.ChildConcept.Equals(childName, StringComparison.OrdinalIgnoreCase) && h.HierarchyType == Models.HierarchyType.IsA)
            .Select(h => h.ParentConcept)
            .ToList();

        // (Phase 15) Provide PART_OF resolver — find child concepts that are PART_OF parentName
        engine.PartOfResolver = (parentName) => hierarchies
            .Where(h => h.ParentConcept.Equals(parentName, StringComparison.OrdinalIgnoreCase) && h.HierarchyType == Models.HierarchyType.PartOf)
            .Select(h => h.ChildConcept)
            .ToList();

        // (Phase 16) Provide Relation resolver
        var relations = _storage.ListRelations(kbName);
        engine.RelationResolver = (name) => relations.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        var result = engine.FindClosure(concept, initialFacts, node.FindVariables);

        if (result.Success && node.SaveResults)
        {
            // Optionally merge starting facts with derived facts and save to DB
            var combinedFacts = new Dictionary<string, object>(initialFacts);
            foreach (var kvp in result.DerivedFacts)
            {
                combinedFacts[kvp.Key] = kvp.Value;
            }

            var obj = new ObjectInstance
            {
                Id = Guid.NewGuid(),
                KbId = kb.Id,
                ConceptName = node.ConceptName,
                Values = combinedFacts
            };
            
            _storage.InsertObject(kbName, obj);
            result.Steps.Add($"Saved derived object instance {obj.Id} to database.");
        }

        return result;
    }

    // ==================== SHOW Handlers ====================

    private object HandleShowKnowledgeBases()
    {
        var kbs = _storage.ListKbs();
        return new { success = true, knowledgeBases = kbs };
    }

    private object HandleShowConcepts(ShowNode node, string kbName)
    {
        var concepts = _storage.ListConcepts(kbName);
        return new { success = true, concepts };
    }

    private object HandleShowConcept(ShowNode node, string kbName)
    {
        var concept = _storage.LoadConcept(kbName, node.ConceptName!);
        return concept != null
            ? new { success = true, concept }
            : new { error = $"Concept '{node.ConceptName}' not found." };
    }

    private object HandleShowRules(ShowNode node, string kbName)
    {
        var rules = _storage.ListRules(kbName);

        if (!string.IsNullOrEmpty(node.RuleType))
        {
            rules = rules.Where(r => r.RuleType?.Equals(node.RuleType, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        return new { success = true, rules };
    }

    private object HandleShowRelations(ShowNode node, string kbName)
    {
        var relations = _storage.ListRelations(kbName);
        return new { success = true, relations };
    }

    private object HandleShowOperators(ShowNode node, string kbName)
    {
        var operators = _storage.ListOperators(kbName);
        return new { success = true, operators };
    }

    private object HandleShowFunctions(ShowNode node, string kbName)
    {
        var functions = _storage.ListFunctions(kbName);
        return new { success = true, functions };
    }

    private object HandleShowHierarchies(ShowNode node, string kbName)
    {
        var hierarchies = _storage.ListHierarchies(kbName);
        return new { success = true, hierarchies };
    }

    private object HandleShowUsers()
    {
        var users = _storage.LoadUsers();
        return new { success = true, users = users.Select(u => new { u.Id, u.Username, u.Role, u.SystemAdmin, u.CreatedAt }) };
    }

    private object HandleShowPrivilegesOnKb(ShowNode node)
    {
        var users = _storage.LoadUsers();
        var privileges = new Dictionary<string, string>();

        foreach (var user in users)
        {
            if (user.KbPrivileges.TryGetValue(node.KbName!, out var priv))
            {
                privileges[user.Username] = priv.ToString();
            }
        }

        return new { success = true, kbName = node.KbName, privileges };
    }

    private object HandleShowPrivilegesOfUser(ShowNode node)
    {
        var users = _storage.LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == node.Username);

        if (user == null)
        {
            return new { error = $"User '{node.Username}' not found." };
        }

        return new { success = true, username = node.Username, privileges = user.KbPrivileges };
    }

    private object HandleAlterConcept(AlterConceptNode node, string kbName)
    {
        var conceptsToAlter = new List<string>();
        if (node.ConceptName == "*")
        {
            conceptsToAlter.AddRange(_storage.ListConcepts(kbName).Select(c => c.Name));
        }
        else
        {
            conceptsToAlter.Add(node.ConceptName);
        }

        foreach (var cName in conceptsToAlter)
        {
            var concept = _storage.LoadConcept(kbName, cName);
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
                }
            }
            _storage.FlushConceptsToDisk(kbName, _storage.ListConcepts(kbName));
            _storage.MigrateConceptInstances(kbName, cName, node.Actions);
        }

        return new { success = true, alteredCount = conceptsToAlter.Count };
    }

    private object HandleAlterKnowledgeBase(AlterKbNode node)
    {
        var kbs = _storage.ListKbs();
        var targets = node.KbName == "*" ? kbs.Select(k => k.Name).ToList() : new List<string> { node.KbName };

        foreach (var name in targets)
        {
            var kb = kbs.FirstOrDefault(k => k.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (kb != null)
            {
                kb.Description = node.NewDescription;
                _storage.SaveKbMetadata(kb);
            }
        }
        return new { success = true, alteredCount = targets.Count };
    }

    private object HandleAlterUser(AlterUserNode node)
    {
        var users = _storage.LoadUsers();
        var user = users.FirstOrDefault(u => u.Username.Equals(node.Username, StringComparison.OrdinalIgnoreCase));
        if (user == null) return new { error = $"User '{node.Username}' not found." };

        if (node.NewPassword != null) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(node.NewPassword);
        if (node.NewAdminStatus.HasValue) user.SystemAdmin = node.NewAdminStatus.Value;

        _storage.SaveUsers(users);
        return new { success = true, username = node.Username };
    }

    private object HandleCreateIndex(KBMS.Parser.Ast.Kdl.CreateIndexNode node, string kbName)
    {
        try {
            _storage.CreateIndex(kbName, node.IndexName, node.ConceptName, node.Variables);
            return new { success = true, message = $"Index '{node.IndexName}' created on '{node.ConceptName}'" };
        } catch (Exception ex) {
            return new { error = ex.Message };
        }
    }

    private object HandleMaintenance(KBMS.Parser.Ast.Kml.MaintenanceNode node, string kbName)
    {
        var results = new List<object>();
        foreach(var action in node.Actions)
        {
            switch(action.ActionType)
            {
                case KBMS.Parser.Ast.Kml.MaintenanceActionType.Vacuum:
                    _storage.Vacuum(kbName);
                    results.Add(new { action = "VACUUM", status = "Completed" });
                    break;
                case KBMS.Parser.Ast.Kml.MaintenanceActionType.Reindex:
                    _storage.Reindex(kbName, action.TargetName);
                    results.Add(new { action = "REINDEX", target = action.TargetName, status = "Completed" });
                    break;
                case KBMS.Parser.Ast.Kml.MaintenanceActionType.CheckConsistency:
                    var issues = _storage.CheckConsistency(kbName, action.TargetName);
                    results.Add(new { action = "CHECK_CONSISTENCY", target = action.TargetName, issues = issues });
                    break;
            }
        }
        return new { success = true, results = results };
    }

    private object HandleExplain(ExplainNode node, string? kbName)
    {
        // For now, just return a mock execution plan
        return new { 
            success = true, 
            plan = new List<string> { 
                "Step 1: Parse query", 
                "Step 2: Check privileges", 
                "Step 3: Analyze " + node.Query.Type,
                "Step 4: Execute on KB " + (kbName ?? "none")
            } 
        };
    }

    private object HandleDescribe(KBMS.Parser.Ast.Kql.DescribeNode node, string kbName)
    {
        switch (node.TargetType.ToUpper())
        {
            case "CONCEPT":
            {
                var concepts = _storage.ListConcepts(kbName);
                var c = concepts.FirstOrDefault(x => x.Name.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase));
                if (c == null) return new { error = $"Concept '{node.TargetName}' not found in KB '{kbName}'" };
                return new
                {
                    success = true,
                    concept = node.TargetName,
                    variables = c.Variables.Select(v => new { v.Name, Type = v.Type }),
                    constraints = c.Constraints.Select(ct => ct.Expression),
                    rules = c.ConceptRules.Select(r => r.Id)
                };
            }
            case "KB":
            {
                var kbs = _storage.ListKbs();
                var kb = kbs.FirstOrDefault(x => x.Name.Equals(kbName, StringComparison.OrdinalIgnoreCase));
                if (kb == null) return new { error = $"Knowledge base '{kbName}' not found." };
                return new
                {
                    success = true,
                    kb = kbName,
                    description = kb.Description,
                    conceptCount = _storage.ListConcepts(kbName).Count,
                    objectCount = kb.ObjectCount
                };
            }
            case "RULE":
            {
                var rules = _storage.ListRules(kbName);
                var r = rules.FirstOrDefault(x => x.Name.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase)
                                               || x.Id.ToString().Equals(node.TargetName, StringComparison.OrdinalIgnoreCase));
                if (r == null) return new { error = $"Rule '{node.TargetName}' not found in KB '{kbName}'" };
                return new
                {
                    success = true,
                    ruleId = r.Id,
                    name = r.Name,
                    type = r.RuleType,
                    scope = r.Scope,
                    hypothesis = r.Hypothesis.Select(h => h.Content),
                    conclusion = r.Conclusion.Select(c => c.Content)
                };
            }
            default:
                return new { error = $"Unknown DESCRIBE target type: {node.TargetType}" };
        }
    }

    private object HandleExport(KBMS.Parser.Ast.Kml.ExportNode node, string kbName)
    {
        try
        {
            var objects = _storage.SelectObjects(kbName)
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
            return new { error = ex.Message };
        }
    }

    private object HandleImport(KBMS.Parser.Ast.Kml.ImportNode node, string kbName)
    {
        try
        {
            if (!File.Exists(node.FilePath))
                return new { error = $"File not found: {node.FilePath}" };

            var json = File.ReadAllText(node.FilePath);
            var imported = System.Text.Json.JsonSerializer.Deserialize<List<KBMS.Models.ObjectInstance>>(json);
            if (imported == null) return new { error = "Failed to deserialize import file" };

            int count = 0;
            foreach (var obj in imported)
            {
                if (node.TargetName != "*" && !obj.ConceptName.Equals(node.TargetName, StringComparison.OrdinalIgnoreCase))
                    continue;
                obj.Id = Guid.NewGuid(); // Assign new ID to avoid collisions
                _storage.InsertObject(kbName, obj);
                count++;
            }

            return new { success = true, imported = count, concept = node.TargetName, file = node.FilePath };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
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
}
