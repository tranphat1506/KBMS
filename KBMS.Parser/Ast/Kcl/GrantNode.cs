using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kcl;

/// <summary>
/// AST node for GRANT statement
/// </summary>
public class GrantNode : KdlNode
{
    /// <summary>
    /// Privilege to grant (READ, WRITE, ADMIN)
    /// </summary>
    public string Privilege { get; set; } = string.Empty;

    /// <summary>
    /// Knowledge Base name
    /// </summary>
    public string KbName { get; set; } = string.Empty;

    /// <summary>
    /// Username to grant privilege to
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
