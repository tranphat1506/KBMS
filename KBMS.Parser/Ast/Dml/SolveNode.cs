namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for SOLVE statement
/// </summary>
public class SolveNode : DmlNode
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Unknown variable to find (FOR clause)
    /// </summary>
    public string FindVariable { get; set; } = string.Empty;

    /// <summary>
    /// Known conditions (GIVEN clause - key=value pairs)
    /// </summary>
    public Dictionary<string, object> Known { get; set; } = new();

    /// <summary>
    /// Optional rule type filter (deduction, default, constraint, computation)
    /// </summary>
    public string? RuleType { get; set; }
}
