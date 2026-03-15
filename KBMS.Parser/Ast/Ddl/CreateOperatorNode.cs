namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE OPERATOR statement
/// </summary>
public class CreateOperatorNode : DdlNode
{
    /// <summary>
    /// Operator symbol (+, -, *, /, ^, etc.)
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// List of parameter types
    /// </summary>
    public List<string> ParamTypes { get; set; } = new();

    /// <summary>
    /// Return type
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Optional properties (commutative, associative, etc.)
    /// </summary>
    public List<string> Properties { get; set; } = new();
}
