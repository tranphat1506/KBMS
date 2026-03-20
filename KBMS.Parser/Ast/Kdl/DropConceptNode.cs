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
}
