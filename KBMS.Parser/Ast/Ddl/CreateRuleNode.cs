namespace KBMS.Parser.Ast;

/// <summary>
/// Rule type enum
/// </summary>
public enum RuleType
{
    Deduction,
    Default,
    Constraint,
    Computation
}

/// <summary>
/// AST node for CREATE RULE statement
/// </summary>
public class CreateRuleNode : DdlNode
{
    /// <summary>
    /// Rule name
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Rule type (deduction, default, constraint, computation)
    /// </summary>
    public RuleType RuleType { get; set; }

    /// <summary>
    /// Scope concept (optional, if null applies to all)
    /// </summary>
    public string? ConceptName { get; set; }

    /// <summary>
    /// Content/description
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// List of condition expressions (IF part)
    /// </summary>
    public List<string> Hypothesis { get; set; } = new();

    /// <summary>
    /// List of conclusion expressions (THEN part)
    /// </summary>
    public List<string> Conclusions { get; set; } = new();

    /// <summary>
    /// Variables used in rule (with types)
    /// </summary>
    public List<VariableDefinition> Variables { get; set; } = new();

    /// <summary>
    /// Optional cost
    /// </summary>
    public int? Cost { get; set; }
}
