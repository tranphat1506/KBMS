namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP USER statement
/// </summary>
public class DropUserNode : DdlNode
{
    /// <summary>
    /// Username to drop
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
