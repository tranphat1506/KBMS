namespace KBMS.Models;

/// <summary>
/// Schema information for a query result set, enabling composability and validation
/// </summary>
public class QuerySchema
{
    /// <summary>
    /// List of columns in the result set
    /// </summary>
    public List<ColumnSchema> Columns { get; set; } = new();

    /// <summary>
    /// The source concept name (if single-table query)
    /// </summary>
    public string? SourceConcept { get; set; }

    /// <summary>
    /// Whether this result can be used as a source for another query
    /// </summary>
    public bool IsComposable { get; set; } = true;

    /// <summary>
    /// Table alias (for JOINs and sub-queries)
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Get a column by name (case-insensitive)
    /// </summary>
    public ColumnSchema? GetColumn(string name)
    {
        return Columns.FirstOrDefault(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            (c.Alias != null && c.Alias.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Check if a column exists (case-insensitive)
    /// </summary>
    public bool HasColumn(string name)
    {
        return GetColumn(name) != null;
    }

    /// <summary>
    /// Create a schema from a concept definition
    /// </summary>
    public static QuerySchema FromConcept(Concept concept, string? alias = null)
    {
        var schema = new QuerySchema
        {
            SourceConcept = concept.Name,
            Alias = alias,
            IsComposable = true
        };

        foreach (var variable in concept.Variables)
        {
            schema.Columns.Add(new ColumnSchema
            {
                Name = variable.Name,
                Type = variable.Type,
                SourceTable = alias ?? concept.Name,
                IsNullable = true, // Default to nullable
                Alias = null
            });
        }

        return schema;
    }

    /// <summary>
    /// Merge two schemas (for JOINs)
    /// </summary>
    public static QuerySchema Merge(QuerySchema left, QuerySchema right, string? leftAlias, string? rightAlias)
    {
        var merged = new QuerySchema
        {
            SourceConcept = null, // JOIN result is not from a single concept
            IsComposable = true
        };

        // Add left columns with prefix
        var leftPrefix = leftAlias ?? left.SourceConcept ?? "left";
        foreach (var col in left.Columns)
        {
            merged.Columns.Add(new ColumnSchema
            {
                Name = col.Name,
                Type = col.Type,
                SourceTable = leftPrefix,
                IsNullable = col.IsNullable,
                Alias = col.Alias,
                PrefixedName = $"{leftPrefix}.{col.Name}"
            });
        }

        // Add right columns with prefix
        var rightPrefix = rightAlias ?? right.SourceConcept ?? "right";
        foreach (var col in right.Columns)
        {
            merged.Columns.Add(new ColumnSchema
            {
                Name = col.Name,
                Type = col.Type,
                SourceTable = rightPrefix,
                IsNullable = col.IsNullable,
                Alias = col.Alias,
                PrefixedName = $"{rightPrefix}.{col.Name}"
            });
        }

        return merged;
    }
}

/// <summary>
/// Schema information for a single column
/// </summary>
public class ColumnSchema
{
    /// <summary>
    /// Column name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data type (INT, STRING, DECIMAL, BOOLEAN, or concept type)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Source table/concept name
    /// </summary>
    public string? SourceTable { get; set; }

    /// <summary>
    /// Whether this column can be null
    /// </summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>
    /// Column alias (if specified with AS)
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Prefixed name (table.column) for disambiguation in JOINs
    /// </summary>
    public string? PrefixedName { get; set; }

    /// <summary>
    /// Whether this is a computed column (expression)
    /// </summary>
    public bool IsComputed { get; set; }

    /// <summary>
    /// Original expression for computed columns
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// Whether this is a SOLVE column (reasoning result)
    /// </summary>
    public bool IsSolveColumn { get; set; }

    /// <summary>
    /// Target variable for SOLVE columns
    /// </summary>
    public string? SolveTarget { get; set; }
}
