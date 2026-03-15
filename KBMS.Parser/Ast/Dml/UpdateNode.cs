namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for UPDATE statement
/// </summary>
public class UpdateNode : DmlNode
{
    /// <summary>
    /// Concept name to update
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Field-value pairs to update (SET clause)
    /// </summary>
    public Dictionary<string, ExpressionNode> SetValues { get; set; } = new();

    /// <summary>
    /// Optional WHERE conditions
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();
}
