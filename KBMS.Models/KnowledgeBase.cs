using System.Collections.Generic;

namespace KBMS.Models;

public class KnowledgeBase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid OwnerId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ObjectCount { get; set; }
    public int RuleCount { get; set; }

    public List<Rule> Rules { get; set; } = new();
    public List<Relation> Relations { get; set; } = new();
    public List<Operator> Operators { get; set; } = new();
    public List<Function> Functions { get; set; } = new();
    public List<Hierarchy> Hierarchies { get; set; } = new();
}
