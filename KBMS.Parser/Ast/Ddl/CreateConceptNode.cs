namespace KBMS.Parser.Ast;

/// <summary>
/// Variable definition in CREATE CONCEPT
/// </summary>
public class VariableDefinition
{
    /// <summary>
    /// Name of variable
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data type (number, string, boolean, object, or SQL types like INT, VARCHAR, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Length for VARCHAR, CHAR types
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Scale for DECIMAL type
    /// </summary>
    public int? Scale { get; set; }
}

/// <summary>
/// Same variable group
/// </summary>
public class SameVariableGroup
{
    /// <summary>
    /// From variable name
    /// </summary>
    public string Var1 { get; set; } = string.Empty;

    /// <summary>
    /// To variable name (equivalent)
    /// </summary>
    public string Var2 { get; set; } = string.Empty;
}

/// <summary>
/// AST node for CREATE CONCEPT statement
/// </summary>
public class CreateConceptNode : DdlNode
{
    /// <summary>
    /// Name of Concept
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// List of variable definitions
    /// </summary>
    public List<VariableDefinition> Variables { get; set; } = new();

    /// <summary>
    /// List of alias names
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// List of base objects
    /// </summary>
    public List<string> BaseObjects { get; set; } = new();

    /// <summary>
    /// List of constraint expressions
    /// </summary>
    public List<string> Constraints { get; set; } = new();

    /// <summary>
    /// List of same variable groups
    /// </summary>
    public List<SameVariableGroup> SameVariables { get; set; } = new();
}
