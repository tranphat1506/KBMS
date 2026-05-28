using System.Collections.Generic;

namespace KBMS.Models;

/// <summary>
/// Central registry for all built-in KBMS functions and macros.
/// This prevents hardcoding functions in the Parser, Lexer, InferenceEngine, and LspEngine.
/// </summary>
public static class BuiltInFunctions
{
    public static readonly IReadOnlyList<string> MathFunctions = new List<string>
    {
        "Abs", "Acos", "Asin", "Atan", "Atan2", "Ceiling", "Cos", "Cosh", "Exp", 
        "Floor", "Log", "Log10", "Max", "Min", "Pow", "Round", "Sign", "Sin", 
        "Sinh", "Sqrt", "Tan", "Tanh", "Truncate"
    }.AsReadOnly();

    public static readonly IReadOnlyList<string> LogicalFunctions = new List<string>
    {
        "if", "and", "or", "not"
    }.AsReadOnly();

    public static readonly IReadOnlyList<string> AggregateFunctions = new List<string>
    {
        "COUNT", "SUM", "AVG", "MAX", "MIN"
    }.AsReadOnly();

    public static readonly IReadOnlyList<string> SystemMacros = new List<string>
    {
        "SOLVE", "CALC", "AGGREGATE"
    }.AsReadOnly();
}
