using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Expressions;

/// <summary>
/// Represents a unary expression (e.g., NOT x, -5)
/// </summary>
public class UnaryExpressionNode : ExpressionNode
{
    /// <summary>
    /// Operator (NOT, -)
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Operand
    /// </summary>
    public ExpressionNode? Operand { get; set; }

    public override string ToString()
    {
        return $"({Operator} {Operand})";
    }
}
