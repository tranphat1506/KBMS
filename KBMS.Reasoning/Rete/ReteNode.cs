using System;
using System.Collections.Generic;
using System.Linq;

namespace KBMS.Reasoning.Rete;

/// <summary>
/// Represents a single fact in the Working Memory.
/// </summary>
public record Fact(string Name, object Value);

/// <summary>
/// A collection of facts that satisfy a partial or full set of rule conditions.
/// </summary>
public class Token
{
    public List<Fact> Facts { get; } = new();

    public Token() { }

    public Token(Fact fact)
    {
        Facts.Add(fact);
    }

    public Token(Token parent, Fact newFact)
    {
        Facts.AddRange(parent.Facts);
        Facts.Add(newFact);
    }

    public object? GetValue(string name) => Facts.LastOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

    public Dictionary<string, object> ToDictionary() => Facts.ToDictionary(f => f.Name, f => f.Value, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Base class for all nodes in the Rete network.
/// </summary>
public abstract class ReteNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public List<ReteNode> Children { get; } = new();

    public virtual void AddChild(ReteNode child)
    {
        if (!Children.Contains(child))
            Children.Add(child);
    }

    /// <summary>
    /// Processes a token entering the node from a parent.
    /// </summary>
    public abstract void ReceiveToken(Token token, ReteNode? sender);

    /// <summary>
    /// Propagates a token to all children.
    /// </summary>
    protected void Propagate(Token token)
    {
        foreach (var child in Children.ToList())
        {
            child.ReceiveToken(token, this);
        }
    }
}

/// <summary>
/// The root node of the Rete network where all facts enter.
/// </summary>
public class EntryNode : ReteNode
{
    public override void ReceiveToken(Token token, ReteNode? sender)
    {
        // Entry node just passes everything through to Alpha nodes
        Propagate(token);
    }

    public void AssertFact(Fact fact)
    {
        ReceiveToken(new Token(fact), null);
    }
}
