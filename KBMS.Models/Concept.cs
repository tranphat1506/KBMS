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
    public string FromVariable { get; set; } = string.Empty;
    public string ToVariable { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public int Cost { get; set; } = 1;
}

public class SameVariable
{
    public string Variable1 { get; set; } = string.Empty;
    public string Variable2 { get; set; } = string.Empty;
}
