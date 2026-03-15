namespace KBMS.Models;

public class Function
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FunctionParameter> Parameters { get; set; } = new();
    public string ReturnType { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> Properties { get; set; } = new();
}

public class FunctionParameter
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
