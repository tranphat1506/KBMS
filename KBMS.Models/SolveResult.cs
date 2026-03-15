using System.Collections.Generic;

namespace KBMS.Models;

public class SolveResult
{
    public bool Success { get; set; }
    public string ConceptName { get; set; } = string.Empty;
    public Dictionary<string, object> DerivedFacts { get; set; } = new();
    public List<string> Steps { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
