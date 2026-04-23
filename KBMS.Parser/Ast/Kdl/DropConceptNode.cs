using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for DROP CONCEPT statement
/// </summary>
public class DropConceptNode : KdlNode
{
    /// <summary>
    /// Name of Concept to drop
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// If true, suppresses error if the Concept does not exist (IF EXISTS clause)
    /// </summary>
    public bool IfExists { get; set; } = false;
}
