namespace KBMS.Parser.Ast;

/// <summary>
/// Parameter definition with name
/// </summary>
public class ParamDefinition
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter type
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
/// AST node for CREATE FUNCTION statement
/// </summary>
public class CreateFunctionNode : DdlNode
{
    /// <summary>
    /// Function name
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// List of parameter definitions (type + name)
    /// </summary>
    public List<ParamDefinition> Parameters { get; set; } = new();

    /// <summary>
    /// Return type
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Function body (formula expression)
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Optional properties
    /// </summary>
    public List<string> Properties { get; set; } = new();
}
