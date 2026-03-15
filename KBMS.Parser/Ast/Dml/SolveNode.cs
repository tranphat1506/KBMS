using System.Collections.Generic;

namespace KBMS.Parser.Ast.Dml;

/// <summary>
/// AST node for SOLVE statement
/// </summary>
public class SolveNode : DmlNode
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Unknown variables to find (FIND clause)
    /// </summary>
    public List<string> FindVariables { get; set; } = new();

    /// <summary>
    /// Known conditions (GIVEN clause - key=value pairs)
    /// </summary>
    public Dictionary<string, string> GivenFacts { get; set; } = new();

    /// <summary>
    /// Whether to save the resulting facts to the database
    /// </summary>
    public bool SaveResults { get; set; }

    public SolveNode(Token token)
    {
        Line = token.Line;
        Column = token.Column;
    }
}
