using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast;
using System.Collections.Generic;

namespace KBMS.Parser.Ast.Kql;

/// <summary>
/// AST node for FIND statement (Semantic Query)
/// FIND <ConceptName> [Alias] [WITH <Conditions>] [RETURN <ReturnItems>]
/// </summary>
public class FindNode : KmlNode
{
    /// <summary>
    /// Concept name to find
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Optional alias for the concept
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Conditions in the WITH clause (Backend Filter)
    /// This may include meta-functions like HAS_FIRED, IS_DEDUCED, etc.
    /// </summary>
    public List<Condition> WithConditions { get; set; } = new();

    /// <summary>
    /// Items in the RETURN clause (Frontend Projection)
    /// This may include meta-functions like AUDIT_LOG, GENERATED_VARIABLES, MISSING_FACTS.
    /// Empty means return the raw object (or default serialization).
    /// </summary>
    public List<SelectColumn> ReturnItems { get; set; } = new();
}
