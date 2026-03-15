namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for USE statement
/// </summary>
public class UseKbNode : DdlNode
{
    /// <summary>
    /// Name of Knowledge Base to use
    /// </summary>
    public string KbName { get; set; } = string.Empty;
}
