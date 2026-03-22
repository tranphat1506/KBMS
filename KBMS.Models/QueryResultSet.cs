using System.Collections.Generic;

namespace KBMS.Models;

public class QueryResultSet
{
    public bool Success { get; set; } = true;
    public string ConceptName { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<ObjectInstance> Objects { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    
    // Optional: for aggregate results or GroupBy
    public List<Dictionary<string, object>> Groups { get; set; } = new();
    public Dictionary<string, object>? Aggregates { get; set; }
}
