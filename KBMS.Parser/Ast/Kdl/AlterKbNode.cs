namespace KBMS.Parser.Ast.Kdl;

public class AlterKbNode : KdlNode
{
    public AlterKbNode() { Type = "ALTER_KNOWLEDGE_BASE"; }
    public new string KbName { get; set; } = string.Empty; // Supports "*"
    public string NewDescription { get; set; } = string.Empty;
}
