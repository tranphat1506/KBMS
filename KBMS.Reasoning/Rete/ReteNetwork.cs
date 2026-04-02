using System;
using System.Collections.Generic;
using System.Linq;

namespace KBMS.Reasoning.Rete;

/// <summary>
/// Manages the full Rete network, including fact assertion, propagation, and agenda management.
/// </summary>
public class ReteNetwork
{
    public EntryNode Root { get; } = new();
    public List<Fact> WorkingMemory { get; } = new();
    public List<(TerminalNode Node, Token Token)> Agenda { get; } = new();
    public Action<string>? Logger { get; set; }

    // Map to keep track of AlphaNodes to share them (Optimization)
    private readonly Dictionary<string, AlphaNode> _alphaNodes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Asserts a new fact into the network.
    /// </summary>
    public void AssertFact(string name, object value)
    {
        // Avoid duplicate facts if value is same
        if (WorkingMemory.ToList().Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && f.Value.Equals(value)))
            return;

        Logger?.Invoke($"Asserting Fact: {name} = {value}");
        var fact = new Fact(name, value);
        WorkingMemory.Add(fact);
        Root.AssertFact(fact);
    }

    /// <summary>
    /// Gets or creates an AlphaNode for a specific variable.
    /// </summary>
    public AlphaNode GetOrCreateAlphaNode(string variableName)
    {
        if (!_alphaNodes.TryGetValue(variableName, out var node))
        {
            node = new AlphaNode(variableName);
            Root.AddChild(node);
            _alphaNodes[variableName] = node;
        }
        return node;
    }

    /// <summary>
    /// Adds an activation to the agenda.
    /// </summary>
    public void AddToAgenda(TerminalNode node, Token token)
    {
        // Simple conflict resolution: don't add duplicate activations for the same rule and same token content
        if (!Agenda.Any(a => a.Node == node && TokensMatch(a.Token, token)))
        {
            Agenda.Add((node, token));
        }
    }

    private bool TokensMatch(Token t1, Token t2)
    {
        if (t1.Facts.Count != t2.Facts.Count) return false;
        for (int i = 0; i < t1.Facts.Count; i++)
        {
            if (t1.Facts[i].Name != t2.Facts[i].Name || !t1.Facts[i].Value.Equals(t2.Facts[i].Value))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Fires one activation from the agenda.
    /// </summary>
    public bool FireNext()
    {
        if (Agenda.Count == 0) return false;

        var (node, token) = Agenda[0];
        Agenda.RemoveAt(0);
        
        Logger?.Invoke($"Firing Rule/Target: {node.RuleName}");
        node.OnActivation(token);
        return true;
    }

    /// <summary>
    /// Clears the network memory.
    /// </summary>
    public void Clear()
    {
        WorkingMemory.Clear();
        Agenda.Clear();
        
        // Note: We don't clear the nodes themselves as they are built once per Concept.
        // But we MUST clear BetaNode memories!
        ClearBetaMemory(Root);
    }

    private void ClearBetaMemory(ReteNode node)
    {
        if (node is BetaNode beta)
        {
            beta.LeftMemory.Clear();
            beta.RightMemory.Clear();
        }
        foreach (var child in node.Children)
        {
            ClearBetaMemory(child);
        }
    }
}
