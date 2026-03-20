using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kcl;

/// <summary>
/// AST node for DROP USER statement
/// </summary>
public class DropUserNode : KdlNode
{
    /// <summary>
    /// Username to drop
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
