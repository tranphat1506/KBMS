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

    // NEW: Schema information for composability
    public QuerySchema? Schema { get; set; }

    // NEW: Error message if not successful
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Convert result set to ObjectInstance list for use as derived table source
    /// </summary>
    /// <param name="alias">Alias for the derived table</param>
    /// <returns>List of ObjectInstances that can be queried</returns>
    public List<ObjectInstance> ToObjectInstances(string alias)
    {
        var result = new List<ObjectInstance>();

        foreach (var obj in Objects)
        {
            // Create new ObjectInstance with prefixed column names
            var newInstance = new ObjectInstance
            {
                Id = obj.Id,
                KbId = obj.KbId,
                ConceptName = alias, // Use alias as concept name
                Values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            };

            // Add values with both prefixed and non-prefixed keys
            foreach (var kv in obj.Values)
            {
                // Keep original key
                newInstance.Values[kv.Key] = kv.Value;

                // Add prefixed key if alias is provided
                if (!string.IsNullOrEmpty(alias))
                {
                    newInstance.Values[$"{alias}.{kv.Key}"] = kv.Value;
                }
            }

            result.Add(newInstance);
        }

        return result;
    }

    /// <summary>
    /// Create an empty result set with error message
    /// </summary>
    public static QueryResultSet Error(string message)
    {
        return new QueryResultSet
        {
            Success = false,
            ErrorMessage = message,
            Objects = new List<ObjectInstance>(),
            Columns = new List<string>()
        };
    }
}
