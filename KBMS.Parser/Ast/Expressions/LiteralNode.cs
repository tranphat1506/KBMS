using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Expressions;

/// <summary>
/// Represents a literal value (number, string, boolean)
/// </summary>
public class LiteralNode : ExpressionNode
{
    /// <summary>
    /// The literal value
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Type of the literal (number, string, boolean)
    /// </summary>
    public string ValueType { get; set; } = string.Empty;

    public override string ToString()
    {
        if (ValueType == "string" && Value != null)
            return $"'{Value}'";
        return Value?.ToString() ?? "null";
    }
}
