namespace KBMS.Parser.Ast;

/// <summary>
/// Represents a binary expression (e.g., a + b, x AND y)
/// </summary>
public class BinaryExpressionNode : ExpressionNode
{
    /// <summary>
    /// Left operand
    /// </summary>
    public ExpressionNode? Left { get; set; }

    /// <summary>
    /// Operator (+, -, *, /, ^, %, =, <>, >, <, >=, <=, AND, OR)
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Right operand
    /// </summary>
    public ExpressionNode? Right { get; set; }

    public override string ToString()
    {
        return $"({Left} {Operator} {Right})";
    }
}
