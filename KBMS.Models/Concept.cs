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
}

public class Variable
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? Length { get; set; }
    public int? Scale { get; set; }
}

public class Constraint
{
    public string Expression { get; set; } = string.Empty;
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
    public string FromConcept { get; set; } = string.Empty;
    public string ToConcept { get; set; } = string.Empty;
}

public class Property
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = null!;
}

public class ConceptRule
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public List<Variable> Variables { get; set; } = new();
    public List<string> Hypothesis { get; set; } = new();
    public List<string> Conclusion { get; set; } = new();
}
