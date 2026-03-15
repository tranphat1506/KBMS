namespace KBMS.Models;

public class ObjectInstance
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string ConceptName { get; set; } = string.Empty;
    public Dictionary<string, object> Values { get; set; } = new();
}
