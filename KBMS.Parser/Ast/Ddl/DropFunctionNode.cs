namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP FUNCTION statement
/// </summary>
public class DropFunctionNode : DdlNode
{
    /// <summary>
    /// Function name to drop
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
}
