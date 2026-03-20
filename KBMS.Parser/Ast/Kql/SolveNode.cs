using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
using System.Collections.Generic;

namespace KBMS.Parser.Ast.Kql;

/// <summary>
/// AST node for SOLVE statement
/// </summary>
public class SolveNode : KmlNode
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
        Type = "SOLVE";
        Line = token.Line;
        Column = token.Column;
    }
}
