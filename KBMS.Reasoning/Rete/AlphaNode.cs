using System;
using System.Collections.Generic;

namespace KBMS.Reasoning.Rete;

/// <summary>
/// An Alpha Node filters tokens based on a simple condition (unary predicate).
/// </summary>
public class AlphaNode : ReteNode
{
    public string VariableName { get; }
    public Func<object, bool>? Condition { get; }

    public AlphaNode(string variableName, Func<object, bool>? condition = null)
    {
        VariableName = variableName;
        Condition = condition;
    }

    public override void ReceiveToken(Token token, ReteNode? sender)
    {
        // Alpha nodes usually receive single-fact tokens from EntryNode
        var fact = token.Facts.LastOrDefault();
        if (fact != null && fact.Name.Equals(VariableName, StringComparison.OrdinalIgnoreCase))
        {
            if (Condition == null || Condition(fact.Value))
            {
                Propagate(token);
            }
        }
    }
}

/// <summary>
/// A Terminal Node represents a fully matched rule or equation.
/// When activated, it triggers an action in the inference engine.
/// </summary>
public class TerminalNode : ReteNode
{
    public string RuleName { get; }
    public Action<Token> OnActivation { get; }

    public TerminalNode(string ruleName, Action<Token> onActivation)
    {
        RuleName = ruleName;
        OnActivation = onActivation;
    }

    public override void ReceiveToken(Token token, ReteNode? sender)
    {
        // A fully matched token has reached the end of the line
        OnActivation(token);
    }
}
