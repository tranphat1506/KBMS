using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for DROP RULE statement
/// </summary>
public class DropRuleNode : KdlNode
{
    /// <summary>
    /// Rule name to drop
    /// </summary>
    public string RuleName { get; set; } = string.Empty;
}
