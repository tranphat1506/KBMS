namespace KBMS.Models;

public class Hierarchy
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string ParentConcept { get; set; } = string.Empty;
    public string ChildConcept { get; set; } = string.Empty;
    public HierarchyType HierarchyType { get; set; }
}

public enum HierarchyType { IsA, PartOf }
