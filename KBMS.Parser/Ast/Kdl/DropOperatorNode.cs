using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for DROP OPERATOR statement
/// </summary>
public class DropOperatorNode : KdlNode
{
    /// <summary>
    /// Operator symbol to drop
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
}
