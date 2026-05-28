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
    public ReteNode? LeftParent { get; set; }
    public ReteNode? RightParent { get; set; }

    public Func<Token, IEnumerable<Token>>? RightDataSource { get; set; }
    public Func<Token, IEnumerable<Token>>? LeftDataSource { get; set; }

    /// <summary>
    /// Receives a token from the LEFT parent.
    /// </summary>
    public void ReceiveLeft(Token leftToken, InferenceSession session)
    {
        var leftMemory = session.GetBetaLeftMemory(Id);
        lock (leftMemory)
        {
            leftMemory.Add(leftToken);
        }

        // Lazy Loading: If RightDataSource is available, fetch dynamically instead of using RightMemory
        if (RightDataSource != null)
        {
            var dynamicRightTokens = RightDataSource(leftToken);
            foreach (var rightToken in dynamicRightTokens)
            {
                if (CanJoin(leftToken, rightToken))
                {
                    Propagate(new Token(leftToken, rightToken), session);
                }
            }
            return;
        }

        // Try to join with every token in RightMemory
        var rightMemory = session.GetBetaRightMemory(Id);
        lock (rightMemory)
        {
            foreach (var rightToken in rightMemory.ToList())
            {
                if (CanJoin(leftToken, rightToken))
                {
                    Propagate(new Token(leftToken, rightToken), session);
                }
            }
        }
    }

    /// <summary>
    /// Receives a token from the RIGHT parent.
    /// </summary>
    public void ReceiveRight(Token rightToken, InferenceSession session)
    {
        var rightMemory = session.GetBetaRightMemory(Id);
        lock (rightMemory)
        {
            rightMemory.Add(rightToken);
        }

        // Lazy Loading: If LeftDataSource is available, fetch dynamically instead of using LeftMemory
        if (LeftDataSource != null)
        {
            var dynamicLeftTokens = LeftDataSource(rightToken);
            foreach (var leftToken in dynamicLeftTokens)
            {
                if (CanJoin(leftToken, rightToken))
                {
                    Propagate(new Token(leftToken, rightToken), session);
                }
            }
            return;
        }

        // Try to join with every token in LeftMemory
        var leftMemory = session.GetBetaLeftMemory(Id);
        lock (leftMemory)
        {
            foreach (var leftToken in leftMemory.ToList())
            {
                if (CanJoin(leftToken, rightToken))
                {
                    Propagate(new Token(leftToken, rightToken), session);
                }
            }
        }
    }

    public override void ReceiveToken(Token token, ReteNode? sender, InferenceSession session)
    {
        if (sender == LeftParent)
        {
            ReceiveLeft(token, session);
        }
        else if (sender == RightParent)
        {
            ReceiveRight(token, session);
        }
        else
        {
            // Fallback for safety, though distributors should call ReceiveLeft/Right directly
            // or we could throw an exception if we want strictness.
        }
    }

    public override void RetractFact(Fact fact, ReteNode? sender, InferenceSession session)
    {
        bool removed = false;

        var leftMemory = session.GetBetaLeftMemory(Id);
        lock (leftMemory)
        {
            int before = leftMemory.Count;
            leftMemory.RemoveAll(t => t.Facts.Any(f => f.Name.Equals(fact.Name, StringComparison.OrdinalIgnoreCase)));
            if (leftMemory.Count < before) removed = true;
        }

        var rightMemory = session.GetBetaRightMemory(Id);
        lock (rightMemory)
        {
            int before = rightMemory.Count;
            rightMemory.RemoveAll(t => t.Facts.Any(f => f.Name.Equals(fact.Name, StringComparison.OrdinalIgnoreCase)));
            if (rightMemory.Count < before) removed = true;
        }

        if (removed || sender == null) 
        {
            PropagateRetract(fact, session);
        }
    }

    public Func<Token, Token, bool>? JoinConditionEvaluator { get; set; }

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

        // Evaluate smart join condition pushed down from compiler
        if (JoinConditionEvaluator != null)
        {
            if (!JoinConditionEvaluator(left, right))
                return false;
        }

        return true;
    }
}
