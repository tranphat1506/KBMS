namespace KBMS.Parser.Ast.Kcl;

public class AlterUserNode : AstNode
{
    public AlterUserNode() { Type = "ALTER_USER"; }
    public string Username { get; set; } = string.Empty;
    public string? NewPassword { get; set; }
    public bool? NewAdminStatus { get; set; }
}
