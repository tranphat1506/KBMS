namespace KBMS.Parser.Ast.Kml;

public class ExportNode : AstNode
{
    public ExportNode() { Type = "EXPORT"; }

    public string TargetType { get; set; } = "CONCEPT"; // Currently only CONCEPT
    public string TargetName { get; set; } = string.Empty; // Name or *
    public string Format { get; set; } = string.Empty; // JSON
    public string FilePath { get; set; } = string.Empty;
}
