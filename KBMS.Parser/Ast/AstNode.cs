namespace KBMS.Parser.Ast;

/// <summary>
/// Base class for all AST nodes
/// </summary>
public abstract class AstNode
{
    /// <summary>
    /// Statement type (e.g., "CREATE_KNOWLEDGE_BASE", "SELECT")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Target Knowledge Base name (if applicable)
    /// </summary>
    public string? KbName { get; set; }

    /// <summary>
    /// Original query string (for debugging)
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// Line number in source (for error reporting)
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number in source (for error reporting)
    /// </summary>
    public int Column { get; set; }
}
