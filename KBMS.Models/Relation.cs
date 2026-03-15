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
}
