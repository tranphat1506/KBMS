namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP KNOWLEDGE BASE statement
/// </summary>
public class DropKbNode : DdlNode
{
    /// <summary>
    /// Name of Knowledge Base to drop
    /// </summary>
    public string KbName { get; set; } = string.Empty;
}
