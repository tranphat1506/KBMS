namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// Represents a DROP TRIGGER statement.
/// </summary>
public class DropTriggerNode : AstNode
{
    public string TriggerName { get; set; } = string.Empty;
}
