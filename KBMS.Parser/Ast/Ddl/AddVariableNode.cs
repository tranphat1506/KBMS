namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for ADD VARIABLE statement
/// </summary>
public class AddVariableNode : DdlNode
{
    /// <summary>
    /// Name of Concept
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Variable name
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// Variable type
    /// </summary>
    public string VariableType { get; set; } = string.Empty;

    /// <summary>
    /// Length for VARCHAR, CHAR types
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Scale for DECIMAL type
    /// </summary>
    public int? Scale { get; set; }
}
