namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// Represents a DROP INDEX statement.
/// </summary>
public class DropIndexNode : AstNode
{
    public string IndexName { get; set; } = string.Empty;
    public string ConceptName { get; set; } = string.Empty;
}
