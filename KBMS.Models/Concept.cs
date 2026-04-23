namespace KBMS.Models;

public class Concept
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Variable> Variables { get; set; } = new();
    public List<Constraint> Constraints { get; set; } = new();
    public List<ComputationRelation> CompRels { get; set; } = new();
    // NEW:
    public List<string> Aliases { get; set; } = new();
    public List<string> BaseObjects { get; set; } = new();
    public List<SameVariable> SameVariables { get; set; } = new();
    public List<ConstructRelation> ConstructRelations { get; set; } = new();
    public List<Property> Properties { get; set; } = new();
    public List<ConceptRule> ConceptRules { get; set; } = new();
    public List<Equation> Equations { get; set; } = new();
}

public class Equation
{
    public Guid Id { get; set; }
    public string Expression { get; set; } = string.Empty;
    public List<string> Variables { get; set; } = new();
    public int Line { get; set; }
    public int Column { get; set; }
}

public class Variable
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? Length { get; set; }
    public int? Scale { get; set; }
    public bool IsReference { get; set; } = false;
    public string? ReferenceConceptName { get; set; }
}

public class Constraint
{
    public string Name { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}

public class ComputationRelation
{
    public Guid Id { get; set; }
    public string ConceptName { get; set; } = string.Empty;
    public int Flag { get; set; }
    public List<string> InputVariables { get; set; } = new();
    public int Rank { get; set; }
    public string? ResultVariable { get; set; }
    public string Expression { get; set; } = string.Empty;
    public int Cost { get; set; } = 1;
}

public class SameVariable
{
    public string Variable1 { get; set; } = string.Empty;
    public string Variable2 { get; set; } = string.Empty;
}

public class ConstructRelation
{
    public string RelationName { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();  // e.g., ["d1", "d2"]
}

public class Property
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = null!;
}

/// <summary>
/// Represents a scope concept in a multi-concept rule
/// </summary>
public class ConceptRuleScopeConcept
{
    public string ConceptName { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public int Position { get; set; }
}

/// <summary>
/// Represents a join condition between concepts in a multi-concept rule
/// </summary>
public class ConceptRuleJoinCondition
{
    public string LeftField { get; set; } = string.Empty;
    public string Operator { get; set; } = "=";
    public string RightField { get; set; } = string.Empty;
}

public class ConceptRule
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;

    // Single concept scope (backward compatibility)
    public string Scope { get; set; } = string.Empty;

    // Multi-concept scope support
    public List<ConceptRuleScopeConcept> ScopeConcepts { get; set; } = new();
    public List<ConceptRuleJoinCondition> JoinConditions { get; set; } = new();

    // Returns true if this is a multi-concept rule
    public bool IsMultiConcept => ScopeConcepts.Count > 1;

    public List<Variable> Variables { get; set; } = new();
    public List<string> Hypothesis { get; set; } = new();
    public List<string> Conclusion { get; set; } = new();
    public int Priority { get; set; } = 50;
}

public enum AlterActionType
{
    AddVariable,
    AddConstraint,
    AddRule,
    AddEquation,
    AddProperty,
    AddConstructRelation,
    DropVariable,
    DropConstraint,
    DropRule,
    DropEquation,
    DropProperty,
    DropConstructRelation,
    RenameVariable
}

public class AlterAction
{
    public AlterActionType ActionType { get; set; }
    public Variable? Variable { get; set; }
    public Constraint? Constraint { get; set; }
    public ConceptRule? Rule { get; set; }
    public Equation? Equation { get; set; }
    public Property? Property { get; set; }
    public ConstructRelation? ConstructRelation { get; set; }
    public string? TargetName { get; set; } // For Drop / Property key
    public string? OldName { get; set; }    // For Rename
    public string? NewName { get; set; }    // For Rename
}
