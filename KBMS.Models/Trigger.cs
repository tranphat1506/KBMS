using System;

namespace KBMS.Models;

public class Trigger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty; // e.g. "Insert", "Update", "Delete"
    public string TargetConcept { get; set; } = string.Empty;
    
    // The original SQL statement that defines this trigger.
    // When loaded from storage, we will re-parse this to reconstruct the AST node.
    public string OriginalQuery { get; set; } = string.Empty; 
}
