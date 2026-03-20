using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

/// <summary>
/// Hierarchy type enum
/// </summary>
public enum HierarchyType
{
    IS_A,
    PART_OF
}

/// <summary>
/// AST node for ADD HIERARCHY statement
/// </summary>
public class AddHierarchyNode : KdlNode
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
