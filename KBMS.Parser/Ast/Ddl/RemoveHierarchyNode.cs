namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for REMOVE HIERARCHY statement
/// </summary>
public class RemoveHierarchyNode : DdlNode
{
    /// <summary>
    /// Parent concept name
    /// </summary>
    public string ParentConcept { get; set; } = string.Empty;

    /// <summary>
    /// Child concept name
    /// </summary>
    public string ChildConcept { get; set; } = string.Empty;

    /// <summary>
    /// Hierarchy type (IS_A or PART_OF)
    /// </summary>
    public HierarchyType HierarchyType { get; set; }
}
