namespace KBMS.Parser.Ast.Kql;

public class DescribeNode : AstNode
{
    public DescribeNode() { Type = "DESCRIBE"; }

    public string TargetType { get; set; } = string.Empty; // CONCEPT, KB, RULE
    public string TargetName { get; set; } = string.Empty; // Name or Id
}
