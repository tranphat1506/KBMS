using System.Collections.Generic;
using KBMS.Models;

namespace KBMS.Parser.Ast.Kdl;

public class AlterConceptNode : KdlNode
{
    public AlterConceptNode() { Type = "ALTER_CONCEPT"; }
    public string ConceptName { get; set; } = string.Empty; // Supports "*"

    public List<KBMS.Models.AlterAction> Actions { get; set; } = new();
}
