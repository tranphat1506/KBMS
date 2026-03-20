using System.Collections.Generic;

namespace KBMS.Parser.Ast.Kdl;

public class CreateIndexNode : AstNode
{
    public CreateIndexNode() { Type = "CREATE_INDEX"; }

    public string IndexName { get; set; } = string.Empty;
    public string ConceptName { get; set; } = string.Empty;
    public List<string> Variables { get; set; } = new();
}
