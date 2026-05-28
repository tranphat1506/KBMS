using System;
using System.Collections.Generic;

namespace KBMS.Reasoning.Rete;

/// <summary>
/// An Alpha Node filters tokens based on a simple condition (unary predicate).
/// </summary>
public class AlphaNode : ReteNode
{
    public string VariableName { get; }
    public string? ConditionExpression { get; }
    public Func<Token, bool>? Condition { get; }

    public AlphaNode(string variableName, string? conditionExpression = null, Func<Token, bool>? condition = null)
    {
        VariableName = variableName;
        ConditionExpression = conditionExpression;
        Condition = condition;
    }

    public override void ReceiveToken(Token token, ReteNode? sender, InferenceSession session)
    {
        var fact = token.Facts.LastOrDefault();
        session.Logger?.Invoke($"[DEBUG] AlphaNode({VariableName}) checking fact {fact?.Name}");
        if (fact != null && fact.Name.Equals(VariableName, StringComparison.OrdinalIgnoreCase))
        {
            var workingToken = token;
            if (fact.Value is Dictionary<string, object> dict)
            {
                var expandedFacts = dict.Select(kv => new Fact($"{VariableName}.{kv.Key}", kv.Value)).ToList();
                workingToken = new Token(expandedFacts);
            }

            if (Condition == null || Condition(workingToken))
            {
                session.Logger?.Invoke($"[DEBUG] AlphaNode({VariableName}) condition passed! Tokens facts: {workingToken.Facts.Count}");
                var memory = session.GetNodeMemory(Id);
                lock (memory)
                {
                    memory.Add(workingToken);
                }
                Propagate(workingToken, session);
            }
            else
            {
                session.Logger?.Invoke($"[DEBUG] AlphaNode({VariableName}) condition FAILED! Tokens facts: {workingToken.Facts.Count}");
            }
        }
    }

    public override void RetractFact(Fact fact, ReteNode? sender, InferenceSession session)
    {
        if (fact.Name.Equals(VariableName, StringComparison.OrdinalIgnoreCase))
        {
            var memory = session.GetNodeMemory(Id);
            var tokenToRemove = memory.FirstOrDefault(t => t.Facts.LastOrDefault()?.Name == fact.Name && t.Facts.LastOrDefault()?.Value.Equals(fact.Value) == true);
            if (tokenToRemove != null)
            {
                lock (memory)
                {
                    memory.Remove(tokenToRemove);
                }
            }

            // We still propagate retract so Beta Nodes can clean up their memories
            PropagateRetract(fact, session);
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
    public Action<Token, InferenceSession> OnActivation { get; }
    public int Cost { get; }
    public int Priority { get; }

    public TerminalNode(string ruleName, Action<Token, InferenceSession> onActivation, int cost = 1, int priority = 50)
    {
        RuleName = ruleName;
        OnActivation = onActivation;
        Cost = cost;
        Priority = priority;
    }

    public override void ReceiveToken(Token token, ReteNode? sender, InferenceSession session)
    {
        // A fully matched token has reached the end of the line
        session.Agenda.AddActivation(new Activation(this, token, Cost, Priority));
    }

    public override void RetractFact(Fact fact, ReteNode? sender, InferenceSession session)
    {
        // Terminal node doesn't propagate further.
        // ReteNetwork removes invalid activations from its Agenda directly.
    }
}
