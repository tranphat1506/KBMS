using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;

namespace KBMS.Reasoning.Rete;

/// <summary>
/// Manages the full Rete network, including fact assertion, propagation, and agenda management.
/// </summary>
public class ReteNetwork
{
    public EntryNode Root { get; } = new();
    public Concept? ContextConcept { get; set; }

    // Map to keep track of AlphaNodes to share them (Optimization)
    // VariableName -> List of AlphaNodes (each representing a different condition for that variable)
    private readonly Dictionary<string, List<AlphaNode>> _alphaNodes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Asserts a new fact into the network. Also synchronizes SameVariables if ContextConcept is provided.
    /// </summary>
    public void AssertFact(string name, object value, InferenceSession session)
    {
        // Avoid duplicate facts if value is same
        if (session.WorkingMemory.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && ValuesEqual(f.Value, value)))
            return;

        var fact = new Fact(name, value);
        session.PendingFacts.Enqueue(fact);

        // SameVariables synchronization
        if (ContextConcept?.SameVariables != null)
        {
            foreach (var sv in ContextConcept.SameVariables)
            {
                if (sv.Variable1.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    session.PendingFacts.Enqueue(new Fact(sv.Variable2, value));
                }
                else if (sv.Variable2.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    session.PendingFacts.Enqueue(new Fact(sv.Variable1, value));
                }
            }
        }

        if (!session.IsPropagating)
        {
            session.IsPropagating = true;
            try
            {
                while (session.PendingFacts.Count > 0)
                {
                    var current = session.PendingFacts.Dequeue();
                    
                    // Re-check WorkingMemory
                    var existing = session.WorkingMemory.FirstOrDefault(f => f.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null) {
                        if (ValuesEqual(existing.Value, current.Value)) continue;
                        // If same name but different value, we must retract first
                        RetractFact(existing.Name, session);
                    }

                    session.Logger?.Invoke($"[Rete] Asserting Fact: {current.Name} = {current.Value}");
                    session.Logger?.Invoke($"[Rete] Propagating {current.Name} to Root. Children count: {Root.Children.Count}");
                    session.WorkingMemory.Add(current);
                    Root.AssertFact(current, session);
                }
            }
            finally
            {
                session.IsPropagating = false;
            }
        }
    }

    /// <summary>
    /// Retracts a fact and removes all derived tokens from the network and agenda.
    /// </summary>
    public void RetractFact(string name, InferenceSession session)
    {
        var factsToRemove = session.WorkingMemory.Where(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var fact in factsToRemove)
        {
            session.Logger?.Invoke($"Retracting Fact: {fact.Name}");
            session.WorkingMemory.Remove(fact);
            Root.RetractFact(fact, null, session);
            
            // Remove any pending activations that rely on this fact
            session.Agenda.RemoveActivationsForFact(fact.Name);
        }

        // Retract synchronized variables as well
        if (ContextConcept?.SameVariables != null)
        {
            foreach (var sv in ContextConcept.SameVariables)
            {
                if (sv.Variable1.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    var syncedFact = session.WorkingMemory.FirstOrDefault(f => f.Name.Equals(sv.Variable2, StringComparison.OrdinalIgnoreCase));
                    if (syncedFact != null)
                    {
                        session.WorkingMemory.Remove(syncedFact);
                        Root.RetractFact(syncedFact, null, session);
                        session.Agenda.RemoveActivationsForFact(syncedFact.Name);
                    }
                }
                else if (sv.Variable2.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    var syncedFact = session.WorkingMemory.FirstOrDefault(f => f.Name.Equals(sv.Variable1, StringComparison.OrdinalIgnoreCase));
                    if (syncedFact != null)
                    {
                        session.WorkingMemory.Remove(syncedFact);
                        Root.RetractFact(syncedFact, null, session);
                        session.Agenda.RemoveActivationsForFact(syncedFact.Name);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates a fact in the network (Retract + Assert).
    /// </summary>
    public void UpdateFact(string name, object value, InferenceSession session)
    {
        RetractFact(name, session);
        AssertFact(name, value, session);
    }

    private bool ValuesEqual(object? v1, object? v2)
    {
        if (v1 == null && v2 == null) return true;
        if (v1 == null || v2 == null) return false;
        
        // Handle numeric equality across types (int, long, double, decimal)
        if (IsNumeric(v1) && IsNumeric(v2))
        {
            return Math.Abs(Convert.ToDouble(v1) - Convert.ToDouble(v2)) < 1e-5;
        }

        return v1.Equals(v2);
    }

    private bool IsNumeric(object v) => v is int or long or double or decimal or float;

    /// <summary>
    /// Gets or creates an AlphaNode for a specific variable and condition.
    /// </summary>
    public AlphaNode GetOrCreateAlphaNode(string variableName, string? conditionExpression = null, Func<Token, bool>? conditionFunc = null)
    {
        if (!_alphaNodes.TryGetValue(variableName, out var nodeList))
        {
            nodeList = new List<AlphaNode>();
            _alphaNodes[variableName] = nodeList;
        }

        // Try to find an existing AlphaNode with the exact same condition expression
        var node = nodeList.FirstOrDefault(n => n.ConditionExpression == conditionExpression);
        if (node == null)
        {
            node = new AlphaNode(variableName, conditionExpression, conditionFunc);
            Root.AddChild(node);
            nodeList.Add(node);
        }
        return node;
    }

    /// <summary>
    /// Fires one activation from the agenda.
    /// </summary>
    public bool FireNext(InferenceSession session)
    {
        var activation = session.Agenda.PopNext();
        if (activation == null) return false;

        session.Logger?.Invoke($"Firing Rule/Target: {activation.Node.RuleName}");
        
        // Track stats
        session.RulesFiredCount++;
        session.InferenceCost += activation.Cost;
        
        activation.Node.OnActivation(activation.Token, session);
        return true;
    }
}
