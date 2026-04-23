using KBMS.Parser.Ast;

namespace KBMS.Parser.Ast.Kql;

public class SearchNode : AstNode
{
    public string Pattern { get; set; } = string.Empty;
}
