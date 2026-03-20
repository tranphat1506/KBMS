using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kml;

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
public class InsertNode : KmlNode
{
    public InsertNode() { Type = "INSERT"; }
    /// <summary>
    /// Concept name to insert into
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Field-value pairs to insert
    /// </summary>
    public Dictionary<string, ValueNode> Values { get; set; } = new();
}
