namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE RELATION statement
/// </summary>
public class CreateRelationNode : DdlNode
{
    /// <summary>
    /// Name of Relation
    /// </summary>
    public string RelationName { get; set; } = string.Empty;

    /// <summary>
    /// Domain concept (source)
    /// </summary>
    public string DomainConcept { get; set; } = string.Empty;

    /// <summary>
    /// Range concept (target)
    /// </summary>
    public string RangeConcept { get; set; } = string.Empty;

    /// <summary>
    /// Optional properties (transitive, symmetric, etc.)
    /// </summary>
    public List<string> Properties { get; set; } = new();
}
