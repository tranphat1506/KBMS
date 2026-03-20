using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// AST node for REMOVE COMPUTATION statement
/// </summary>
public class RemoveComputationNode : KdlNode
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Result variable to remove
    /// </summary>
    public string VariableName { get; set; } = string.Empty;
}
