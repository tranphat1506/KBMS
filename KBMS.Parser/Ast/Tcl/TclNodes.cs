using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
namespace KBMS.Parser.Ast.Tcl;

/// <summary>TCL: BEGIN TRANSACTION</summary>
public class BeginTransactionNode : AstNode
{
    public BeginTransactionNode()
    {
        Type = "BEGIN_TRANSACTION";
    }
}

/// <summary>TCL: COMMIT</summary>
public class CommitNode : AstNode
{
    public CommitNode()
    {
        Type = "COMMIT";
    }
}

/// <summary>TCL: ROLLBACK</summary>
public class RollbackNode : AstNode
{
    public RollbackNode()
    {
        Type = "ROLLBACK";
    }
}
