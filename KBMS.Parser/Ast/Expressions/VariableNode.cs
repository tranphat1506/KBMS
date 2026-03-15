namespace KBMS.Parser.Ast;

/// <summary>
/// Represents a variable reference
/// </summary>
public class VariableNode : ExpressionNode
{
    /// <summary>
    /// Variable name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional concept alias (e.g., ConceptName.VariableName)
    /// </summary>
    public string? ConceptAlias { get; set; }

    public override string ToString()
    {
        return ConceptAlias != null ? $"{ConceptAlias}.{Name}" : Name;
    }
}
