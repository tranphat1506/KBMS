namespace KBMS.Models;

public class Operator
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public List<string> ParamTypes { get; set; } = new();
    public string ReturnType { get; set; } = string.Empty;
    public List<string> Properties { get; set; } = new();
}
