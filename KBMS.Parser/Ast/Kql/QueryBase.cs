using KBMS.Parser.Ast.Expressions;
using System.Collections.Generic;

namespace KBMS.Parser.Ast.Kql;

/// <summary>
/// Condition in WHERE/HAVING clause
/// </summary>
public class Condition
{
    /// <summary>
    /// Field/variable name
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Comparison operator (=, <>, >, <, >=, <=)
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Value to compare
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Optional logical operator for chaining (AND, OR)
    /// </summary>
    public string? LogicalOperator { get; set; }
}

/// <summary>
/// JOIN clause
/// </summary>
public class JoinClause
{
    /// <summary>
    /// Type of join: INNER, LEFT, RIGHT (default: INNER)
    /// </summary>
    public string JoinType { get; set; } = "INNER";

    /// <summary>
    /// Concept or relation name to join
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Optional alias for the joined concept
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Join condition (ON clause)
    /// </summary>
    public Condition? OnCondition { get; set; }
}

/// <summary>
/// Aggregation function clause
/// </summary>
public class AggregateClause
{
    /// <summary>
    /// Type of aggregation: COUNT, SUM, AVG, MAX, MIN
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Variable to aggregate (null for COUNT(*))
    /// </summary>
    public string? Variable { get; set; }

    /// <summary>
    /// Optional alias for the aggregate result
    /// </summary>
    public string? Alias { get; set; }
}

/// <summary>
/// ORDER BY clause item
/// </summary>
public class OrderByItem
{
    /// <summary>
    /// Variable to order by
    /// </summary>
    public string Variable { get; set; } = string.Empty;

    /// <summary>
    /// Order direction: ASC or DESC (default: ASC)
    /// </summary>
    public string Direction { get; set; } = "ASC";
}

/// <summary>
/// LIMIT clause
/// </summary>
public class LimitClause
{
    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Number of results to skip (OFFSET)
    /// </summary>
    public int? Offset { get; set; }
}
