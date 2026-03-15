namespace KBMS.Parser.Ast;

/// <summary>
/// Represents a function call (e.g., sqrt(x), COUNT(*))
/// </summary>
public class FunctionCallNode : ExpressionNode
{
    /// <summary>
    /// Function name
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// List of arguments
    /// </summary>
    public List<ExpressionNode> Arguments { get; set; } = new();

    public override string ToString()
    {
        return $"{FunctionName}({string.Join(", ", Arguments)})";
    }
}
