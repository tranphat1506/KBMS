using System;
using System.Collections.Generic;
using System.Linq;

namespace KBMS.Reasoning.Rete;

public class Agenda
{
    private readonly List<Activation> _activations = new();
    public int MaxFireLimit { get; set; } = 1000;
    private int _fireCount = 0;

    public void AddActivation(Activation activation)
    {
        // Simple conflict resolution: avoid adding exactly duplicate activations
        if (!_activations.Any(a => a.Node == activation.Node && TokensMatch(a.Token, activation.Token)))
        {
            _activations.Add(activation);
        }
    }

    public Activation? PopNext()
    {
        if (_activations.Count == 0) return null;

        if (_fireCount >= MaxFireLimit)
        {
            throw new Exception($"Rule Avalanche Detected: Maximum fire limit of {MaxFireLimit} reached.");
        }

        // Conflict Resolution:
        // 1. Lowest Cost fires first
        // 2. If costs are equal, Highest Priority fires first
        var next = _activations
            .OrderBy(a => a.Cost)
            .ThenByDescending(a => a.Priority)
            .First();

        _activations.Remove(next);
        _fireCount++;

        return next;
    }

    public void RemoveActivationsForFact(string factName)
    {
        _activations.RemoveAll(a => a.Token.Facts.Any(f => f.Name.Equals(factName, StringComparison.OrdinalIgnoreCase)));
    }

    public void Clear()
    {
        _activations.Clear();
        _fireCount = 0;
    }

    public int Count => _activations.Count;

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
}
