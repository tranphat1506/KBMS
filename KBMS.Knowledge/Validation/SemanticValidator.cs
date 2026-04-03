using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Parser.Ast;
using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Expressions;
using KBMS.Storage.V3;

namespace KBMS.Knowledge.Validation;

/// <summary>
/// Result of semantic validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

/// <summary>
/// Semantic validator for KBQL queries
/// Validates concept existence, variable membership, type compatibility, etc.
/// </summary>
public class SemanticValidator
{
    private readonly ConceptCatalog _conceptCatalog;
    private readonly KbCatalog _kbCatalog;

    public SemanticValidator(ConceptCatalog conceptCatalog, KbCatalog kbCatalog)
    {
        _conceptCatalog = conceptCatalog;
        _kbCatalog = kbCatalog;
    }

    /// <summary>
    /// Validate a SELECT statement
    /// </summary>
    public ValidationResult ValidateSelect(SelectNode node, string kbName)
    {
        var result = new ValidationResult();

        // 1. Validate target concept exists
        var conceptName = node.ConceptName.Split('.')[0]; // Handle Concept.subTarget
        if (!string.IsNullOrEmpty(conceptName) && conceptName != "*")
        {
            var concept = _conceptCatalog.LoadConcept(kbName, conceptName);
            if (concept == null)
            {
                // Check if it's a special target type
                if (node.TargetType?.ToUpper() != "HIERARCHY" &&
                    node.TargetType?.ToUpper() != "RULE" &&
                    node.TargetType?.ToUpper() != "RELATION" &&
                    node.TargetType?.ToUpper() != "FUNCTION" &&
                    node.TargetType?.ToUpper() != "OPERATOR")
                {
                    result.AddError($"Concept '{conceptName}' not found in knowledge base '{kbName}'.");
                    return result;
                }
            }
            else
            {
                // 2. Validate SELECT columns
                foreach (var col in node.SelectColumns)
                {
                    if (col.IsStar) continue;

                    ValidateColumnReference(col, concept, node.Alias, result);
                }

                // 3. Validate WHERE conditions
                foreach (var condition in node.Conditions)
                {
                    ValidateCondition(condition, concept, node.Alias, result);
                }

                // 4. Validate JOINs
                foreach (var join in node.Joins)
                {
                    ValidateJoin(join, kbName, concept, node.Alias, result);
                }

                // 5. Validate GROUP BY variables
                foreach (var groupVar in node.GroupBy)
                {
                    if (!VariableExists(concept, groupVar))
                    {
                        result.AddWarning($"GROUP BY variable '{groupVar}' not found in concept '{conceptName}'.");
                    }
                }

                // 6. Validate ORDER BY variables
                foreach (var orderBy in node.OrderBy)
                {
                    if (!VariableExists(concept, orderBy.Variable))
                    {
                        result.AddWarning($"ORDER BY variable '{orderBy.Variable}' not found in concept '{conceptName}'.");
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Validate an INSERT statement
    /// </summary>
    public ValidationResult ValidateInsert(InsertNode node, string kbName)
    {
        var result = new ValidationResult();

        // 1. Validate concept exists
        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        if (concept == null)
        {
            result.AddError($"Concept '{node.ConceptName}' not found in knowledge base '{kbName}'.");
            return result;
        }

        // 2. Validate value keys exist in concept variables
        foreach (var kv in node.Values)
        {
            var variable = concept.Variables.FirstOrDefault(v =>
                v.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));

            if (variable == null)
            {
                result.AddWarning($"INSERT key '{kv.Key}' not found in concept '{node.ConceptName}' variables.");
            }
            else
            {
                // 3. Validate type compatibility
                ValidateTypeCompatibility(kv.Key, variable.Type, kv.Value.ValueType, result);
            }
        }

        return result;
    }

    /// <summary>
    /// Validate an UPDATE statement
    /// </summary>
    public ValidationResult ValidateUpdate(UpdateNode node, string kbName)
    {
        var result = new ValidationResult();

        // 1. Validate concept exists
        var concept = _conceptCatalog.LoadConcept(kbName, node.ConceptName);
        if (concept == null)
        {
            result.AddError($"Concept '{node.ConceptName}' not found in knowledge base '{kbName}'.");
            return result;
        }

        // 2. Validate SET variables exist in concept
        foreach (var setItem in node.SetValues)
        {
            if (!VariableExists(concept, setItem.Key))
            {
                result.AddError($"Variable '{setItem.Key}' not found in concept '{node.ConceptName}'.");
            }
        }

        // 3. Validate WHERE conditions
        foreach (var condition in node.Conditions)
        {
            ValidateCondition(condition, concept, null, result);
        }

        return result;
    }

    /// <summary>
    /// Validate a CREATE RULE statement
    /// </summary>
    public ValidationResult ValidateRule(CreateRuleNode node, string kbName)
    {
        var result = new ValidationResult();

        // 1. Validate scope concept exists (if specified)
        var scope = node.ConceptName;
        Concept? concept = null;

        if (!string.IsNullOrEmpty(scope))
        {
            concept = _conceptCatalog.LoadConcept(kbName, scope);
            if (concept == null)
            {
                result.AddError($"Rule scope concept '{scope}' not found in knowledge base '{kbName}'.");
                return result;
            }
        }

        // 2. Try to infer scope from hypothesis if not specified
        if (concept == null && node.Hypothesis.Count > 0)
        {
            var firstHyp = node.Hypothesis[0].ToString();
            var match = System.Text.RegularExpressions.Regex.Match(firstHyp ?? "", @"^(\w+)\(");
            if (match.Success)
            {
                scope = match.Groups[1].Value;
                concept = _conceptCatalog.LoadConcept(kbName, scope);
                if (concept == null)
                {
                    result.AddError($"Inferred scope concept '{scope}' from hypothesis not found in knowledge base '{kbName}'.");
                    return result;
                }
            }
        }

        // 3. Validate hypothesis variables if concept found
        if (concept != null)
        {
            foreach (var hyp in node.Hypothesis)
            {
                ValidateExpressionVariables(hyp, concept, result);
            }

            // 4. Validate conclusion variables
            foreach (var conc in node.Conclusions)
            {
                ValidateExpressionVariables(conc, concept, result);
            }
        }

        return result;
    }

    /// <summary>
    /// Validate an ADD HIERARCHY statement
    /// </summary>
    public ValidationResult ValidateHierarchy(AddHierarchyNode node, string kbName)
    {
        var result = new ValidationResult();

        // 1. Validate child concept exists
        var childConcept = _conceptCatalog.LoadConcept(kbName, node.ChildConcept);
        if (childConcept == null)
        {
            result.AddError($"Child concept '{node.ChildConcept}' not found in knowledge base '{kbName}'.");
        }

        // 2. Validate parent concept exists
        var parentConcept = _conceptCatalog.LoadConcept(kbName, node.ParentConcept);
        if (parentConcept == null)
        {
            result.AddError($"Parent concept '{node.ParentConcept}' not found in knowledge base '{kbName}'.");
        }

        // 3. Check for cycle (child == parent)
        if (node.ChildConcept.Equals(node.ParentConcept, StringComparison.OrdinalIgnoreCase))
        {
            result.AddError($"Hierarchy cycle detected: concept '{node.ChildConcept}' cannot be its own parent.");
        }

        // 4. Check for existing cycle in hierarchy chain
        if (childConcept != null && parentConcept != null)
        {
            if (HasHierarchyCycle(kbName, node.ChildConcept, node.ParentConcept))
            {
                result.AddError($"Hierarchy cycle detected: adding '{node.ChildConcept}' IS_A '{node.ParentConcept}' would create a cycle.");
            }
        }

        return result;
    }

    /// <summary>
    /// Validate a CREATE RELATION statement
    /// </summary>
    public ValidationResult ValidateRelation(CreateRelationNode node, string kbName)
    {
        var result = new ValidationResult();

        // 1. Validate domain concept exists
        if (!string.IsNullOrEmpty(node.DomainConcept))
        {
            var domainConcept = _conceptCatalog.LoadConcept(kbName, node.DomainConcept);
            if (domainConcept == null)
            {
                result.AddError($"Domain concept '{node.DomainConcept}' not found in knowledge base '{kbName}'.");
            }
        }

        // 2. Validate range concept exists
        if (!string.IsNullOrEmpty(node.RangeConcept))
        {
            var rangeConcept = _conceptCatalog.LoadConcept(kbName, node.RangeConcept);
            if (rangeConcept == null)
            {
                result.AddError($"Range concept '{node.RangeConcept}' not found in knowledge base '{kbName}'.");
            }
        }

        return result;
    }

    #region Private Helpers

    private void ValidateColumnReference(SelectColumn col, Concept concept, string? alias, ValidationResult result)
    {
        var name = col.Name;

        // Skip if it's SELECT *
        if (col.IsStar) return;

        // Skip if it's an expression (will be validated at execution)
        if (col.Expression != null)
        {
            // Check if it's a SOLVE function
            if (col.Expression is FunctionCallNode func && func.FunctionName.Equals("SOLVE", StringComparison.OrdinalIgnoreCase))
            {
                var targetVar = func.Arguments.FirstOrDefault()?.ToString();
                if (!string.IsNullOrEmpty(targetVar) && !VariableExists(concept, targetVar))
                {
                    result.AddWarning($"SOLVE target variable '{targetVar}' not found in concept '{concept.Name}'.");
                }
            }
            return;
        }

        // Check if variable exists in concept
        if (!VariableExists(concept, name))
        {
            result.AddWarning($"Column '{name}' not found in concept '{concept.Name}'.");
        }
    }

    private void ValidateCondition(Condition condition, Concept concept, string? alias, ValidationResult result)
    {
        // Check field exists
        var fieldName = condition.Field;
        if (!VariableExists(concept, fieldName))
        {
            result.AddWarning($"Condition field '{fieldName}' not found in concept '{concept.Name}'.");
        }

        // Check if value is a sub-query
        if (condition.Value is SelectNode subQuery)
        {
            // Recursively validate sub-query
            var subResult = ValidateSelect(subQuery, concept.Name); // Use current KB
            if (!subResult.IsValid)
            {
                result.AddError($"Sub-query validation failed: {string.Join(", ", subResult.Errors)}");
            }
        }
    }

    private void ValidateJoin(JoinClause join, string kbName, Concept leftConcept, string? leftAlias, ValidationResult result)
    {
        // 1. Validate target concept exists
        var rightConcept = _conceptCatalog.LoadConcept(kbName, join.Target);
        if (rightConcept == null)
        {
            result.AddError($"JOIN target concept '{join.Target}' not found in knowledge base '{kbName}'.");
            return;
        }

        // 2. Validate ON condition fields
        if (join.OnCondition != null)
        {
            // Check left field
            if (!VariableExists(leftConcept, join.OnCondition.Field))
            {
                result.AddWarning($"JOIN condition field '{join.OnCondition.Field}' not found in concept '{leftConcept.Name}'.");
            }

            // Check right field (if it's a variable reference)
            var rightField = join.OnCondition.Value?.ToString();
            if (!string.IsNullOrEmpty(rightField) && !VariableExists(rightConcept, rightField))
            {
                result.AddWarning($"JOIN condition field '{rightField}' not found in concept '{join.Target}'.");
            }
        }
    }

    private void ValidateExpressionVariables(ExpressionNode expr, Concept concept, ValidationResult result)
    {
        if (expr == null) return;

        switch (expr)
        {
            case VariableNode varNode:
                if (!VariableExists(concept, varNode.Name))
                {
                    result.AddWarning($"Variable '{varNode.Name}' not found in concept '{concept.Name}'.");
                }
                break;

            case BinaryExpressionNode binary:
                ValidateExpressionVariables(binary.Left, concept, result);
                ValidateExpressionVariables(binary.Right, concept, result);
                break;

            case UnaryExpressionNode unary:
                ValidateExpressionVariables(unary.Operand, concept, result);
                break;

            case FunctionCallNode func:
                foreach (var arg in func.Arguments)
                {
                    ValidateExpressionVariables(arg, concept, result);
                }
                break;
        }
    }

    private void ValidateTypeCompatibility(string varName, string varType, string? valueType, ValidationResult result)
    {
        if (string.IsNullOrEmpty(valueType)) return;

        var vt = varType.ToUpper();
        var vt2 = valueType.ToUpper();

        // Basic type compatibility checks
        var numericTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INT", "INTEGER", "LONG", "BIGINT", "FLOAT", "DOUBLE", "DECIMAL", "NUMBER"
        };

        var stringTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "STRING", "VARCHAR", "CHAR", "TEXT"
        };

        var boolTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BOOL", "BOOLEAN"
        };

        if (numericTypes.Contains(vt) && !numericTypes.Contains(vt2) && vt2 != "NULL")
        {
            result.AddWarning($"Type mismatch: variable '{varName}' is {varType}, but value is {valueType}.");
        }
        else if (stringTypes.Contains(vt) && !stringTypes.Contains(vt2) && vt2 != "NULL")
        {
            result.AddWarning($"Type mismatch: variable '{varName}' is {varType}, but value is {valueType}.");
        }
        else if (boolTypes.Contains(vt) && !boolTypes.Contains(vt2) && vt2 != "NULL")
        {
            result.AddWarning($"Type mismatch: variable '{varName}' is {varType}, but value is {valueType}.");
        }
    }

    private bool VariableExists(Concept concept, string variableName)
    {
        if (string.IsNullOrEmpty(variableName)) return false;

        // Handle dot notation (e.g., "p1.x")
        if (variableName.Contains('.'))
        {
            var parts = variableName.Split('.');
            if (parts.Length != 2) return false;

            var prefix = parts[0];
            var field = parts[1];

            // Find the variable with this prefix
            var prefixVar = concept.Variables.FirstOrDefault(v =>
                v.Name.Equals(prefix, StringComparison.OrdinalIgnoreCase));

            if (prefixVar == null) return false;

            // If it's a concept type, check if field exists
            if (!IsPrimitiveType(prefixVar.Type))
            {
                var subConcept = _conceptCatalog.LoadConcept(concept.KbId.ToString(), prefixVar.Type);
                if (subConcept != null)
                {
                    return subConcept.Variables.Any(v => v.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
                }
            }

            return false;
        }

        // Direct variable check
        return concept.Variables.Any(v => v.Name.Equals(variableName, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsPrimitiveType(string type)
    {
        var primitives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INT", "INTEGER", "LONG", "BIGINT", "TINYINT", "SMALLINT",
            "FLOAT", "DOUBLE", "DECIMAL", "NUMBER", "MONEY",
            "STRING", "VARCHAR", "CHAR", "TEXT",
            "BOOL", "BOOLEAN",
            "DATE", "DATETIME", "TIMESTAMP",
            "OBJECT"
        };
        return primitives.Contains(type);
    }

    private bool HasHierarchyCycle(string kbName, string child, string newParent)
    {
        var kb = _kbCatalog.LoadKb(kbName);
        if (kb == null) return false;

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return CheckAncestorsForCycle(kb, newParent, child, visited);
    }

    private bool CheckAncestorsForCycle(KnowledgeBase kb, string current, string target, HashSet<string> visited)
    {
        if (current.Equals(target, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!visited.Add(current))
            return false; // Already visited, no cycle through this path

        // Get parents of current
        var parents = kb.Hierarchies
            .Where(h => h.ChildConcept.Equals(current, StringComparison.OrdinalIgnoreCase) &&
                        h.HierarchyType == Models.HierarchyType.IsA)
            .Select(h => h.ParentConcept);

        foreach (var parent in parents)
        {
            if (CheckAncestorsForCycle(kb, parent, target, visited))
                return true;
        }

        return false;
    }

    #endregion
}
