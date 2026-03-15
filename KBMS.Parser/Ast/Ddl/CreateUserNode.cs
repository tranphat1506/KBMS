namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE USER statement
/// </summary>
public class CreateUserNode : DdlNode
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
