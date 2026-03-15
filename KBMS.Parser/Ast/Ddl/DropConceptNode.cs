namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP CONCEPT statement
/// </summary>
public class DropConceptNode : DdlNode
{
    /// <summary>
    /// Name of Concept to drop
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;
}
