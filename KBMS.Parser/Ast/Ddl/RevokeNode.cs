namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for REVOKE statement
/// </summary>
public class RevokeNode : DdlNode
{
    /// <summary>
    /// Privilege to revoke (READ, WRITE, ADMIN)
    /// </summary>
    public string Privilege { get; set; } = string.Empty;

    /// <summary>
    /// Knowledge Base name
    /// </summary>
    public string KbName { get; set; } = string.Empty;

    /// <summary>
    /// Username to revoke privilege from
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
