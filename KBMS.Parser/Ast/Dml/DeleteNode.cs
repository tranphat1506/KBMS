namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DELETE statement
/// </summary>
public class DeleteNode : DmlNode
{
    /// <summary>
    /// Concept name to delete from
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Optional WHERE conditions
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();
}
