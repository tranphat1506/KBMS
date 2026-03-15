namespace KBMS.Models;

public class Rule
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Name { get; set; } = string.Empty;
    // NEW:
    public string RuleType { get; set; } = "deduction";  // deduction, default, constraint, computation
    public string Scope { get; set; } = string.Empty;  // concept name
    public int Cost { get; set; } = 1;
    public List<Expression> Hypothesis { get; set; } = new();
    public List<Expression> Conclusion { get; set; } = new();
}

public class Expression
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<Expression> Children { get; set; } = new();
}
