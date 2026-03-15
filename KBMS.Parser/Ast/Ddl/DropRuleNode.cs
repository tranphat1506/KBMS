namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP RULE statement
/// </summary>
public class DropRuleNode : DdlNode
{
    /// <summary>
    /// Rule name to drop
    /// </summary>
    public string RuleName { get; set; } = string.Empty;
}
