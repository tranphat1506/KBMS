using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
using System.Collections.Generic;

namespace KBMS.Parser.Ast.Kql;

/// <summary>
/// AST node for SELECT statement
/// </summary>
public class SelectNode : KmlNode
{
    /// <summary>
    /// Type of entity to select from (CONCEPT, RELATION, RULE, etc.)
    /// </summary>
    public string TargetType { get; set; } = "CONCEPT";

    /// <summary>
    /// Concept name to select from (stores entity name or system.table)
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Optional alias for the main concept
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Optional WHERE conditions
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();

    /// <summary>
    /// Optional JOIN clauses
    /// </summary>
    public List<JoinClause> Joins { get; set; } = new();

    /// <summary>
    /// Optional aggregation functions
    /// </summary>
    public List<AggregateClause> Aggregates { get; set; } = new();

    /// <summary>
    /// Optional GROUP BY variables
    /// </summary>
    public List<string> GroupBy { get; set; } = new();

    /// <summary>
    /// Optional HAVING clause (filter after aggregation)
    /// </summary>
    public Condition? Having { get; set; }

    /// <summary>
    /// Optional ORDER BY clause
    /// </summary>
    public List<OrderByItem> OrderBy { get; set; } = new();

    /// <summary>
    /// Optional LIMIT clause
    /// </summary>
    public LimitClause? Limit { get; set; }
}
