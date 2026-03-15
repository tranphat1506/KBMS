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

    /// <summary>
    /// Parameter names for the relation (e.g., ["a", "b"])
    /// </summary>
    public List<string> ParamNames { get; set; } = new();

    /// <summary>
    /// Equations that define the relation's knowledge
    /// </summary>
    public List<EquationDef> Equations { get; set; } = new();

    /// <summary>
    /// Rules embedded in the relation
    /// </summary>
    public List<ConceptRuleDef> ConceptRules { get; set; } = new();
}
