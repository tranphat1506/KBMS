using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for DROP FUNCTION statement
/// </summary>
public class DropFunctionNode : KdlNode
{
    /// <summary>
    /// Function name to drop
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
}
