using System;
using System.Collections.Generic;
using System.Linq;

namespace KBMS.Reasoning.Rete;

/// <summary>
/// Represents a single fact in the Working Memory.
/// </summary>
public record Fact(string Name, object Value);

public class ReasoningStep
{
    public string RuleName { get; set; } = string.Empty;
    public int StepCost { get; set; }
    public Dictionary<string, object> InputFacts { get; set; } = new();
    public Dictionary<string, object> OutputFacts { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Logic { get; set; }
    public List<string> UsedVariables { get; set; } = new();
}

public class ExplanationNode
{
    public string Goal { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? DerivedBy { get; set; }
    public bool IsBaseFact { get; set; }
    public string? Logic { get; set; }
    public int? StepCost { get; set; }
    public List<ExplanationNode>? Dependencies { get; set; }
}

/// <summary>
/// A collection of facts that satisfy a partial or full set of rule conditions.
/// </summary>
public class Token
{
    public List<Fact> Facts { get; } = new();
    public List<ReasoningStep> AuditTrail { get; } = new();
    public List<string> GeneratedVariables { get; } = new();

    public Token() { }

    public Token(IEnumerable<Fact> facts)
    {
        Facts.AddRange(facts);
    }

    public Token(Fact fact)
    {
        Facts.Add(fact);
    }

    public Token(Token parent, Fact newFact)
    {
        Facts.AddRange(parent.Facts);
        Facts.Add(newFact);
        AuditTrail.AddRange(parent.AuditTrail);
        GeneratedVariables.AddRange(parent.GeneratedVariables);
    }

    // Constructor to merge two tokens (BetaNode Join)
    public Token(Token left, Token right)
    {
        Facts.AddRange(left.Facts);
        // Avoid duplicate facts from right side
        var leftNames = new HashSet<string>(left.Facts.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        Facts.AddRange(right.Facts.Where(f => !leftNames.Contains(f.Name)));
        
        AuditTrail.AddRange(left.AuditTrail);
        AuditTrail.AddRange(right.AuditTrail);
        
        GeneratedVariables.AddRange(left.GeneratedVariables);
        GeneratedVariables.AddRange(right.GeneratedVariables);
        
        // Remove duplicates in audit/generated if any
        GeneratedVariables = GeneratedVariables.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        AuditTrail = AuditTrail.DistinctBy(a => a.Timestamp).ToList(); // Naive dedup
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
    public abstract void ReceiveToken(Token token, ReteNode? sender, InferenceSession session);

    /// <summary>
    /// Processes the retraction of a fact from the node.
    /// </summary>
    public abstract void RetractFact(Fact fact, ReteNode? sender, InferenceSession session);

    /// <summary>
    /// Propagates a token to all children.
    /// </summary>
    protected void Propagate(Token token, InferenceSession session)
    {
        foreach (var child in Children.ToList())
        {
            child.ReceiveToken(token, this, session);
        }
    }

    /// <summary>
    /// Propagates the retraction to all children.
    /// </summary>
    protected void PropagateRetract(Fact fact, InferenceSession session)
    {
        foreach (var child in Children.ToList())
        {
            child.RetractFact(fact, this, session);
        }
    }
}

/// <summary>
/// The root node of the Rete network where all facts enter.
/// </summary>
public class EntryNode : ReteNode
{
    public override void ReceiveToken(Token token, ReteNode? sender, InferenceSession session)
    {
        // Entry node just passes everything through to Alpha nodes
        Propagate(token, session);
    }

    public override void RetractFact(Fact fact, ReteNode? sender, InferenceSession session)
    {
        PropagateRetract(fact, session);
    }

    public void AssertFact(Fact fact, InferenceSession session)
    {
        ReceiveToken(new Token(fact), null, session);
    }
}
