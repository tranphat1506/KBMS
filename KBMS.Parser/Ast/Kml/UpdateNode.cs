using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kml;

/// <summary>
/// AST node for UPDATE statement
/// </summary>
public class UpdateNode : KmlNode
{
    /// <summary>
    /// Concept name to update
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Field-value pairs to update (SET clause)
    /// </summary>
    public Dictionary<string, ExpressionNode> SetValues { get; set; } = new();

    /// <summary>
    /// Optional WHERE conditions
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();
}
