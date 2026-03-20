namespace KBMS.Parser.Ast.Kdl;

public enum TriggerEvent { Insert, Update, Delete }

public class CreateTriggerNode : AstNode
{
    public CreateTriggerNode() { Type = "CREATE_TRIGGER"; }

    public string TriggerName { get; set; } = string.Empty;
    public TriggerEvent Event { get; set; }
    public string TargetConcept { get; set; } = string.Empty; // Concept name or *
    public AstNode? Action { get; set; }   // The DO block AST node (e.g. SolveNode, UpdateNode)
}
