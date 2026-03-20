using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for DROP RELATION statement
/// </summary>
public class DropRelationNode : KdlNode
{
    /// <summary>
    /// Name of Relation to drop
    /// </summary>
    public string RelationName { get; set; } = string.Empty;
}
