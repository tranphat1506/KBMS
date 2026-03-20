using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;

namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for CREATE KNOWLEDGE BASE statement
/// </summary>
public class CreateKbNode : KdlNode
{
    public CreateKbNode() { Type = "CREATE_KNOWLEDGE_BASE"; }
    /// <summary>
    /// Name of Knowledge Base
    /// </summary>
    public new string KbName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
}
