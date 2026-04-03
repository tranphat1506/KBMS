namespace KBMS.Models;

/// <summary>
/// Represents a scope concept in a multi-concept rule
/// </summary>
public class RuleScopeConcept
{
    public string ConceptName { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public int Position { get; set; }
}

/// <summary>
/// Represents a join condition between concepts in a multi-concept rule
/// </summary>
public class RuleJoinCondition
{
    public string LeftField { get; set; } = string.Empty;
    public string Operator { get; set; } = "=";
    public string RightField { get; set; } = string.Empty;
}

public class Rule
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Rule type: deduction, default, constraint, computation
    public string RuleType { get; set; } = "deduction";

    // Single concept scope (backward compatibility)
    public string Scope { get; set; } = string.Empty;

    // Multi-concept scope support
    public List<RuleScopeConcept> ScopeConcepts { get; set; } = new();
    public List<RuleJoinCondition> JoinConditions { get; set; } = new();

    // Returns true if this is a multi-concept rule
    public bool IsMultiConcept => ScopeConcepts.Count > 1;

    public int Cost { get; set; } = 1;
    public int Priority { get; set; } = 50;  // For conflict resolution
    public List<Expression> Hypothesis { get; set; } = new();
    public List<Expression> Conclusion { get; set; } = new();

    /// <summary>
    /// Get alias for a concept at given position, returns concept name if no alias
    /// </summary>
    public string GetAliasOrName(int position)
    {
        if (position >= 0 && position < ScopeConcepts.Count)
        {
            return ScopeConcepts[position].Alias ?? ScopeConcepts[position].ConceptName;
        }
        return Scope;
    }
}

public class Expression
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<Expression> Children { get; set; } = new();
}
