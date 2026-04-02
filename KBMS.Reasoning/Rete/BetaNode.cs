using System;
using System.Collections.Generic;
using System.Linq;

namespace KBMS.Reasoning.Rete;

/// <summary>
/// A Beta Node joins results from a left parent (previous partial match)
/// and a right parent (new alpha condition).
/// </summary>
public class BetaNode : ReteNode
{
    // Memory tables for partial matches
    public List<Token> LeftMemory { get; } = new();
    public List<Token> RightMemory { get; } = new();

    public ReteNode? LeftParent { get; set; }
    public ReteNode? RightParent { get; set; }

    /// <summary>
    /// Receives a token from the LEFT parent.
    /// </summary>
    public void ReceiveLeft(Token leftToken)
    {
        lock (LeftMemory)
        {
            LeftMemory.Add(leftToken);
        }

        // Try to join with every token in RightMemory
        lock (RightMemory)
        {
            foreach (var rightToken in RightMemory.ToList())
            {
                if (CanJoin(leftToken, rightToken))
                {
                    Propagate(new Token(leftToken, rightToken.Facts.Last()));
                }
            }
        }
    }

    /// <summary>
    /// Receives a token from the RIGHT parent.
    /// </summary>
    public void ReceiveRight(Token rightToken)
    {
        lock (RightMemory)
        {
            RightMemory.Add(rightToken);
        }

        // Try to join with every token in LeftMemory
        lock (LeftMemory)
        {
            foreach (var leftToken in LeftMemory.ToList())
            {
                if (CanJoin(leftToken, rightToken))
                {
                    Propagate(new Token(leftToken, rightToken.Facts.Last()));
                }
            }
        }
    }

    public override void ReceiveToken(Token token, ReteNode? sender)
    {
        if (sender == LeftParent)
        {
            ReceiveLeft(token);
        }
        else if (sender == RightParent)
        {
            ReceiveRight(token);
        }
        else
        {
            // Fallback for safety, though distributors should call ReceiveLeft/Right directly
            // or we could throw an exception if we want strictness.
        }
    }

    private bool CanJoin(Token left, Token right)
    {
        // For simple KBMS rules, we just ensure no conflicting variables.
        // In a more complex Rete, we might check consistency constraints.
        var rightFact = right.Facts.LastOrDefault();
        if (rightFact == null) return false;
        
        // Ensure no conflicting fact with same name but different value in history
        var existing = left.Facts.FirstOrDefault(f => f.Name.Equals(rightFact.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null && !existing.Value.Equals(rightFact.Value))
            return false;

        return true;
    }
}
