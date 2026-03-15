namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for ADD COMPUTATION statement
/// </summary>
public class AddComputationNode : DdlNode
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// List of input variables
    /// </summary>
    public List<string> InputVariables { get; set; } = new();

    /// <summary>
    /// Result variable
    /// </summary>
    public string ResultVariable { get; set; } = string.Empty;

    /// <summary>
    /// Formula expression
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// Optional cost/weight
    /// </summary>
    public int? Cost { get; set; }
}
