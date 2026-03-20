namespace KBMS.Parser.Ast.Kql;

public class ExplainNode : AstNode
{
    public ExplainNode() { Type = "EXPLAIN"; }
    public AstNode Query { get; set; } = null!;
}
