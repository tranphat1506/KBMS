namespace KBMS.Parser.Ast;

/// <summary>
/// Value node for INSERT VALUES
/// </summary>
public class ValueNode
{
    /// <summary>
    /// Type of value (number, string, boolean, identifier)
    /// </summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>
    /// The value
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// AST node for INSERT statement
/// </summary>
public class InsertNode : DmlNode
{
    /// <summary>
    /// Concept name to insert into
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Field-value pairs to insert
    /// </summary>
    public Dictionary<string, ValueNode> Values { get; set; } = new();
}
