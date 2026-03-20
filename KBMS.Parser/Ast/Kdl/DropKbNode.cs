using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for DROP KNOWLEDGE BASE statement
/// </summary>
public class DropKbNode : KdlNode
{
    /// <summary>
    /// Name of Knowledge Base to drop
    /// </summary>
    public string KbName { get; set; } = string.Empty;
}
