using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;

namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for USE statement
/// </summary>
public class UseKbNode : KdlNode
{
    /// <summary>
    /// Name of Knowledge Base to use
    /// </summary>
    public new string KbName { get; set; } = string.Empty;
}
