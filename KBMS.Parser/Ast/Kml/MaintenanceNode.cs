using System.Collections.Generic;

namespace KBMS.Parser.Ast.Kml;

public enum MaintenanceActionType
{
    Vacuum,
    Reindex,
    CheckConsistency
}

public class MaintenanceAction
{
    public MaintenanceActionType ActionType { get; set; }
    
    // For REINDEX and CHECK CONSISTENCY
    public string TargetName { get; set; } = string.Empty; // ConceptName or *
}

public class MaintenanceNode : AstNode
{
    public MaintenanceNode() { Type = "MAINTENANCE"; }

    public List<MaintenanceAction> Actions { get; set; } = new();
}
