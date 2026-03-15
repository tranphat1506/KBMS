namespace KBMS.Models;

public class Relation
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    // NEW:
    public List<string> Properties { get; set; } = new();  // transitive, symmetric, reflexive, etc.
    // Phase 16: Relation knowledge
    public List<string> ParamNames { get; set; } = new();  // e.g., ["a", "b"]
    public List<Equation> Equations { get; set; } = new();
    public List<ConceptRule> Rules { get; set; } = new();
}
