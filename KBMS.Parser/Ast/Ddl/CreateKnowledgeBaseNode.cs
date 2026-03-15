namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE KNOWLEDGE BASE statement
/// </summary>
public class CreateKbNode : DdlNode
{
    /// <summary>
    /// Name of Knowledge Base
    /// </summary>
    public string KbName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
}
