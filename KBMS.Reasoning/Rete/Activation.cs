namespace KBMS.Reasoning.Rete;

public class Activation
{
    public TerminalNode Node { get; }
    public Token Token { get; }
    public int Cost { get; }
    public int Priority { get; }

    public Activation(TerminalNode node, Token token, int cost, int priority)
    {
        Node = node;
        Token = token;
        Cost = cost;
        Priority = priority;
    }
}
