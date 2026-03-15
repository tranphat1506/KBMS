using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Parser.Ast;
using KBMS.Parser.Ast.Dml;
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

            _ => new { error = $"Unknown command type: {ast.Type}" }
        };
    }

    // ==================== DDL Handlers ====================

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
        var concept = new Concept
        {
            Name = node.ConceptName,
            Variables = node.Variables.Select(v => new Variable
            {
                Name = v.Name,
                Type = v.Type,
                Length = v.Length,
                Scale = v.Scale
            }).ToList(),
            Aliases = node.Aliases,
            BaseObjects = node.BaseObjects,
            Constraints = node.Constraints.Select(c => new Constraint { Expression = c }).ToList(),
            SameVariables = node.SameVariables.Select(sv => new SameVariable
            {
                Variable1 = sv.Var1,
                Variable2 = sv.Var2
            }).ToList(),
            ConstructRelations = node.ConstructRelations.Select(cr => new ConstructRelation
            {
                RelationName = cr.RelationName,
                FromConcept = cr.FromConcept,
                ToConcept = cr.ToConcept
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
            }).ToList()
        };

        var created = _storage.CreateConcept(kbName, concept);
        return created != null
            ? new { success = true, message = $"Concept '{node.ConceptName}' created successfully." }
            : new { error = $"Concept '{node.ConceptName}' already exists." };
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
            Properties = node.Properties
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
        var op = new Operator
        {
            Symbol = node.Symbol,
            ParamTypes = node.ParamTypes,
            ReturnType = node.ReturnType,
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
        var objects = _storage.SelectObjects(kbName, conditions.Count > 0 ? conditions : null);

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
}
