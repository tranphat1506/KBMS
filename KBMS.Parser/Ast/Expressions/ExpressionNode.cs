using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Expressions;

/// <summary>
/// Base class for all expression nodes
/// </summary>
public abstract class ExpressionNode
{
    /// <summary>
    /// Line number in source (for error reporting)
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number in source (for error reporting)
    /// </summary>
    public int Column { get; set; }
}
