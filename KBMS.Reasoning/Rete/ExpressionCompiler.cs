using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace KBMS.Reasoning.Rete;

public class ExpressionCompiler
{
    private static readonly Dictionary<string, Func<Dictionary<string, object>, bool>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static Func<Token, bool> CompileCondition(string expression, InferenceEngine engine)
    {
        var nativeDelegate = TryCompileNative(expression, engine);
        
        if (nativeDelegate != null)
        {
            return token => {
                try {
                    return nativeDelegate(token.ToDictionary());
                } catch {
                    return FallbackEvaluate(expression, token, engine);
                }
            };
        }
        
        return token => FallbackEvaluate(expression, token, engine);
    }

    private static bool FallbackEvaluate(string expression, Token token, InferenceEngine engine)
    {
        try {
            return engine.EvaluateConstraint(expression, token.ToDictionary());
        } catch {
            return false;
        }
    }

    private static Func<Dictionary<string, object>, bool>? TryCompileNative(string expression, InferenceEngine engine)
    {
        if (_cache.TryGetValue(expression, out var cached))
        {
            return cached;
        }

        try
        {
            var vars = engine.ExtractVariablesFromExpression(expression);
            
            // Preprocess expression for Dynamic Linq
            string safeExpr = Regex.Replace(expression, @"'([^']*)'", "\"$1\""); // Single quotes to double quotes for strings
            safeExpr = Regex.Replace(safeExpr, @"(?<![><!=])=(?!=)", "=="); // '=' to '=='
            safeExpr = Regex.Replace(safeExpr, @"\b(and)\b", "&&", RegexOptions.IgnoreCase);
            safeExpr = Regex.Replace(safeExpr, @"\b(or)\b", "||", RegexOptions.IgnoreCase);

            // To support dynamic dictionary execution, we'll compile a Lambda that takes individual typed parameters.
            // But since we don't know the exact type, we will assume double for math, and string for strings.
            // Dynamic Linq allows 'object' parameters but math on objects fails. 
            // So we will create a dynamic Lambda taking 'object[]' or just use Dynamic Linq's `it` if we pass a strongly typed object.
            
            // Wait, Dynamic Linq supports `DynamicClass` which is super fast.
            // We can just construct a `System.Linq.Expressions.Expression` manually for simple cases to guarantee speed,
            // or just use Dynamic Linq with `it`.

            // Let's create an array of ParameterExpressions for each variable as `object`? No, as `dynamic`?
            // Dynamic Linq doesn't support `dynamic` keyword easily without Microsoft.CSharp.

            // Simplest native approach using Dynamic Linq:
            // We replace `sys` with `Convert.ToDouble(it["sys"])` if it's compared to a number, but that's hard to parse.
            
            // Let's try to parse the Lambda using NCalc as fallback, but build a REAL Native Expression Tree manually!
            // We'll write a simple tokenizer and builder for basic patterns like "A > B and C == 'Admin'".
            // If it falls outside our basic patterns, we return null and fallback to NCalc.

            var delegateFunc = BuildSimpleNativeExpression(safeExpr, vars);
            if (delegateFunc != null)
            {
                _cache[expression] = delegateFunc;
                return delegateFunc;
            }
            
            return null;
        }
        catch
        {
            return null; // Fallback to NCalc
        }
    }

    /// <summary>
    /// Builds a Native C# Expression Tree for simple relational and logical conditions.
    /// This is hundreds of times faster than NCalc because it compiles directly to native IL.
    /// </summary>
    private static Func<Dictionary<string, object>, bool>? BuildSimpleNativeExpression(string expr, List<string> vars)
    {
        // Parameter: facts
        var factsParam = Expression.Parameter(typeof(Dictionary<string, object>), "facts");
        
        // This parser handles: A > 5 && B == "Admin"
        var tokens = Tokenize(expr);
        if (tokens.Count == 0) return null;

        var body = ParseOr(tokens, factsParam, vars);
        if (body == null) return null;

        var lambda = Expression.Lambda<Func<Dictionary<string, object>, bool>>(body, factsParam);
        return lambda.Compile();
    }

    private static Expression? ParseOr(List<string> tokens, ParameterExpression factsParam, List<string> vars)
    {
        var left = ParseAnd(tokens, factsParam, vars);
        if (left == null) return null;

        while (tokens.Count > 0 && tokens[0] == "||")
        {
            tokens.RemoveAt(0);
            var right = ParseAnd(tokens, factsParam, vars);
            if (right == null) return null;
            left = Expression.OrElse(left, right);
        }
        return left;
    }

    private static Expression? ParseAnd(List<string> tokens, ParameterExpression factsParam, List<string> vars)
    {
        var left = ParseCondition(tokens, factsParam, vars);
        if (left == null) return null;

        while (tokens.Count > 0 && tokens[0] == "&&")
        {
            tokens.RemoveAt(0);
            var right = ParseCondition(tokens, factsParam, vars);
            if (right == null) return null;
            left = Expression.AndAlso(left, right);
        }
        return left;
    }

    private static Expression? ParseCondition(List<string> tokens, ParameterExpression factsParam, List<string> vars)
    {
        if (tokens.Count < 3) return null;

        string leftStr = tokens[0];
        string op = tokens[1];
        string rightStr = tokens[2];

        if (!IsOperator(op)) return null;

        tokens.RemoveAt(0);
        tokens.RemoveAt(0);
        tokens.RemoveAt(0);

        Expression leftExpr = ParseOperand(leftStr, factsParam, vars);
        Expression rightExpr = ParseOperand(rightStr, factsParam, vars);

        if (leftExpr == null || rightExpr == null) return null;

        // Ensure both sides are double for numeric comparison
        if (op == ">" || op == "<" || op == ">=" || op == "<=")
        {
            leftExpr = EnsureDouble(leftExpr);
            rightExpr = EnsureDouble(rightExpr);
        }
        else if (op == "==" || op == "!=")
        {
            // If one is string and another is string, we should use object.Equals or string.Compare
            // To keep it simple, we use Object.Equals
            var equalsMethod = typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) })!;
            var eqExpr = Expression.Call(equalsMethod, EnsureObject(leftExpr), EnsureObject(rightExpr));
            
            if (op == "!=") return Expression.Not(eqExpr);
            return eqExpr;
        }

        return op switch
        {
            ">" => Expression.GreaterThan(leftExpr, rightExpr),
            "<" => Expression.LessThan(leftExpr, rightExpr),
            ">=" => Expression.GreaterThanOrEqual(leftExpr, rightExpr),
            "<=" => Expression.LessThanOrEqual(leftExpr, rightExpr),
            _ => null
        };
    }

    private static Expression ParseOperand(string opStr, ParameterExpression factsParam, List<string> vars)
    {
        if (double.TryParse(opStr, out double d))
        {
            return Expression.Constant(d, typeof(double));
        }
        if (opStr.StartsWith("\"") && opStr.EndsWith("\""))
        {
            return Expression.Constant(opStr.Trim('"'), typeof(string));
        }
        if (opStr == "true") return Expression.Constant(true, typeof(bool));
        if (opStr == "false") return Expression.Constant(false, typeof(bool));

        // Variable
        if (vars.Contains(opStr, StringComparer.OrdinalIgnoreCase))
        {
            var keyExpr = Expression.Constant(opStr);
            var indexerMethod = typeof(Dictionary<string, object>).GetMethod("get_Item")!;
            var callExpr = Expression.Call(factsParam, indexerMethod, keyExpr);
            return callExpr; // Returns object
        }

        return null!;
    }

    private static Expression EnsureDouble(Expression expr)
    {
        if (expr.Type == typeof(double)) return expr;
        var convertMethod = typeof(Convert).GetMethod("ToDouble", new[] { typeof(object) })!;
        return Expression.Call(convertMethod, EnsureObject(expr));
    }

    private static Expression EnsureObject(Expression expr)
    {
        if (expr.Type == typeof(object)) return expr;
        return Expression.Convert(expr, typeof(object));
    }

    private static bool IsOperator(string op) => op is ">" or "<" or ">=" or "<=" or "==" or "!=";

    private static List<string> Tokenize(string expr)
    {
        var tokens = new List<string>();
        var pattern = @"(>=|<=|==|!=|>|<|&&|\|\||\""[^\""]*\""|[a-zA-Z_][a-zA-Z0-9_]*|\d+(?:\.\d+)?)";
        foreach (Match match in Regex.Matches(expr, pattern))
        {
            tokens.Add(match.Value);
        }
        return tokens;
    }
}
