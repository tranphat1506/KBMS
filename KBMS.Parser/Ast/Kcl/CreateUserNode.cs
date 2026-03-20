using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kcl;

/// <summary>
/// AST node for CREATE USER statement
/// </summary>
public class CreateUserNode : KdlNode
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password (will be hashed)
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User role (ROOT or USER, default: USER)
    /// </summary>
    public string Role { get; set; } = "USER";

    /// <summary>
    /// System admin flag
    /// </summary>
    public bool SystemAdmin { get; set; }
}
