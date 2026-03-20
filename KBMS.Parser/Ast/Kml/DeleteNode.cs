using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kml;

/// <summary>
/// AST node for DELETE statement
/// </summary>
public class DeleteNode : KmlNode
{
    /// <summary>
    /// Concept name to delete from
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Optional WHERE conditions
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();
}
