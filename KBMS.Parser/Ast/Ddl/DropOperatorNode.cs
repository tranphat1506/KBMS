namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP OPERATOR statement
/// </summary>
public class DropOperatorNode : DdlNode
{
    /// <summary>
    /// Operator symbol to drop
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
}
