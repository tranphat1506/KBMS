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

    /// <summary>
    /// List of construct relations
    /// </summary>
    public List<ConstructRelationDef> ConstructRelations { get; set; } = new();

    /// <summary>
    /// List of concept properties
    /// </summary>
    public List<PropertyDef> Properties { get; set; } = new();

    /// <summary>
    /// List of local rules
    /// </summary>
    public List<ConceptRuleDef> ConceptRules { get; set; } = new();

    /// <summary>
    /// List of equations
    /// </summary>
    public List<EquationDef> Equations { get; set; } = new();
}

/// <summary>
/// Definition for an equation bound to a concept
/// </summary>
public class EquationDef
{
    public string Expression { get; set; } = string.Empty;
}

/// <summary>
/// Definition for a rule bound to a concept
/// </summary>
public class ConceptRuleDef
{
    public string Kind { get; set; } = string.Empty;
    public List<VariableDefinition> Variables { get; set; } = new();
    public List<string> Hypothesis { get; set; } = new();
    public List<string> Conclusion { get; set; } = new();
}

/// <summary>
/// Definition for a property string-value pair
/// </summary>
public class PropertyDef
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Definition for a construct relation
/// </summary>
public class ConstructRelationDef
{
    public string RelationName { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();  // e.g., ["d1", "d2"]
}
