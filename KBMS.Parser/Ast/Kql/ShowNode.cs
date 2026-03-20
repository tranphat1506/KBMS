using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Kql;

/// <summary>
/// Type of SHOW command
/// </summary>
public enum ShowType
{
    KnowledgeBases,
    Concepts,
    ConceptDetail,
    Rules,
    Relations,
    Operators,
    Functions,
    Hierarchies,
    Users,
    PrivilegesOnKb,
    PrivilegesOfUser
}

/// <summary>
/// AST node for SHOW statement
/// </summary>
public class ShowNode : KmlNode
{
    /// <summary>
    /// Type of information to show
    /// </summary>
    public ShowType ShowType { get; set; }

    /// <summary>
    /// Optional KB name (for SHOW CONCEPTS IN <kb>)
    /// </summary>
    public string? KbName { get; set; }

    /// <summary>
    /// Optional concept name (for SHOW CONCEPT <name>)
    /// </summary>
    public string? ConceptName { get; set; }

    /// <summary>
    /// Optional rule type filter (for SHOW RULES TYPE <type>)
    /// </summary>
    public string? RuleType { get; set; }

    /// <summary>
    /// Optional username (for SHOW PRIVILEGES OF <user>)
    /// </summary>
    public string? Username { get; set; }
}
