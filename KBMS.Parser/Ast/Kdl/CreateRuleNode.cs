using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kdl;

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
/// Represents a scope concept in a multi-concept rule (AST level)
/// </summary>
public class AstRuleScopeConcept
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Optional alias (e.g., "Patient p" -> alias = "p")
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Position in scope list (0 = first, 1 = second, etc.)
    /// </summary>
    public int Position { get; set; }
}

/// <summary>
/// AST node for CREATE RULE statement
/// </summary>
public class CreateRuleNode : KdlNode
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
    /// Primary scope concept (for backward compatibility with single-concept rules)
    /// </summary>
    public string? ConceptName { get; set; }

    /// <summary>
    /// Multi-concept scope list (for multi-concept rules)
    /// </summary>
    public List<AstRuleScopeConcept> ScopeConcepts { get; set; } = new();

    /// <summary>
    /// Join conditions between scope concepts (for multi-concept rules)
    /// E.g., "Patient.id = LabResult.patientId"
    /// </summary>
    public List<Condition> JoinConditions { get; set; } = new();

    /// <summary>
    /// Returns true if this is a multi-concept rule
    /// </summary>
    public bool IsMultiConcept => ScopeConcepts.Count > 1;

    /// <summary>
    /// Content/description
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// List of condition expressions (IF part)
    /// </summary>
    public List<ExpressionNode> Hypothesis { get; set; } = new();

    /// <summary>
    /// List of conclusion expressions (THEN part)
    /// </summary>
    public List<ExpressionNode> Conclusions { get; set; } = new();

    /// <summary>
    /// Variables used in rule (with types)
    /// </summary>
    public List<VariableDefinition> Variables { get; set; } = new();

    /// <summary>
    /// Optional cost
    /// </summary>
    public int? Cost { get; set; }

    /// <summary>
    /// Optional priority (for conflict resolution)
    /// </summary>
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Get alias for a concept, returns concept name if no alias
    /// </summary>
    public string GetAliasOrName(int position)
    {
        if (position >= 0 && position < ScopeConcepts.Count)
        {
            return ScopeConcepts[position].Alias ?? ScopeConcepts[position].ConceptName;
        }
        return ConceptName ?? "";
    }
}
