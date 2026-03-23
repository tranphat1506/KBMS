using System.Collections.Generic;
namespace KBMS.Parser.Ast.Kml;

/// <summary>
/// AST node for INSERT BULK statement — inserts multiple rows in one command.
/// Syntax: INSERT BULK INTO <Concept> ATTRIBUTES (k:v, ...), (k:v, ...), ...;
/// </summary>
public class InsertBulkNode : KmlNode
{
    public InsertBulkNode() { Type = "INSERT_BULK"; }

    /// <summary>
    /// Concept name to insert into.
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Each element is one row of field-value pairs to insert.
    /// Positional values use keys "_0", "_1", etc.
    /// </summary>
    public List<Dictionary<string, ValueNode>> Rows { get; set; } = new();
}
