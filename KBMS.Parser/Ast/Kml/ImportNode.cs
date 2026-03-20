namespace KBMS.Parser.Ast.Kml;

public class ImportNode : AstNode
{
    public ImportNode() { Type = "IMPORT"; }

    public string TargetType { get; set; } = "CONCEPT";
    public string TargetName { get; set; } = string.Empty; // Name or *
    public string Format { get; set; } = string.Empty; // JSON
    public string FilePath { get; set; } = string.Empty;
}
