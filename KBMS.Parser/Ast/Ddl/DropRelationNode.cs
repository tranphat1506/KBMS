namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP RELATION statement
/// </summary>
public class DropRelationNode : DdlNode
{
    /// <summary>
    /// Name of Relation to drop
    /// </summary>
    public string RelationName { get; set; } = string.Empty;
}
