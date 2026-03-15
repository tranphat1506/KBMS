namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for REMOVE COMPUTATION statement
/// </summary>
public class RemoveComputationNode : DdlNode
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Result variable to remove
    /// </summary>
    public string VariableName { get; set; } = string.Empty;
}
