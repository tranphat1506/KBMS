using System;
using System.Linq;
using System.Collections.Generic;
using KBMS.Models;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Expressions;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Knowledge.V3.Optimizer;

/// <summary>
/// Compiles a KBQL AST Expression into a functional predicate that can be executed 
/// directly on binary V3 Tuples during a scan or join.
/// 
/// Handles Field 1 mapping (dynamic schema) to ensure field names like "age" 
/// are correctly mapped to physical tuple indices.
/// </summary>
public static class PredicateCompiler
{
    public static Func<Tuple, bool> Compile(List<Condition>? conditions)
    {
        if (conditions == null || conditions.Count == 0) return (t) => true;

        return (tuple) => 
        {
            if (tuple.Fields.Count < 2) return false;
            var fieldNames = tuple.GetString(1).Split('|').ToList();

            foreach (var cond in conditions)
            {
                if (!EvaluateCondition(cond, tuple, fieldNames)) return false; 
            }
            return true;
        };
    }

    private static bool EvaluateCondition(Condition cond, Tuple tuple, List<string> fieldNames)
    {
        var val = GetFieldValue(cond.Field, tuple, fieldNames);
        return cond.Operator switch
        {
            "=" or "==" => Equals(val, cond.Value),
            ">" => Compare(val, cond.Value) > 0,
            "<" => Compare(val, cond.Value) < 0,
            ">=" => Compare(val, cond.Value) >= 0,
            "<=" => Compare(val, cond.Value) <= 0,
            "!=" or "<>" => !Equals(val, cond.Value),
            _ => false
        };
    }

    private static object? GetFieldValue(string fieldName, Tuple tuple, List<string> fieldNames)
    {
        int idx = fieldNames.FindIndex(n => n.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0 && (idx + 2) < tuple.Fields.Count)
        {
            return tuple.GetString(idx + 2);
        }
        return null;
    }


    private static object? GetValue(ExpressionNode node, Tuple tuple, List<string> fieldNames)
    {
        if (node is LiteralNode literal) return literal.Value;
        
        if (node is VariableNode variable)
        {
            // Map name to index. Tuple values start at index 2.
            int idx = fieldNames.FindIndex(n => n.Equals(variable.Name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && (idx + 2) < tuple.Fields.Count)
            {
                return tuple.GetString(idx + 2);
            }
        }

        return null;
    }

    private static int Compare(object? a, object? b)
    {
        if (a == null || b == null) return 0;

        // Try numeric comparison
        if (double.TryParse(a.ToString(), out double da) && double.TryParse(b.ToString(), out double db))
        {
            return da.CompareTo(db);
        }

        // Fallback to string
        return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static new bool Equals(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        
        if (double.TryParse(a.ToString(), out double da) && double.TryParse(b.ToString(), out double db))
        {
            return Math.Abs(da - db) < 0.000001;
        }

        return a.ToString()!.Equals(b.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
