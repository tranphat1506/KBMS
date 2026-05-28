using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Reasoning;
using KBMS.Reasoning.Rete;
using KBMS.Models;
using Xunit;

namespace KBMS.Tests;

public class BetaNodeSmartJoinTests
{
    [Fact]
    public void BetaNode_ShouldEvaluateJoinConditionAndPreventCartesianProduct()
    {
        // 1. Arrange
        var network = new ReteNetwork();
        var session = new InferenceSession();
        var engine = new InferenceEngine();
        
        var alphaA = network.GetOrCreateAlphaNode("A");
        var alphaB = network.GetOrCreateAlphaNode("B");

        var beta = new BetaNode();
        beta.LeftParent = alphaA;
        beta.RightParent = alphaB;

        alphaA.AddChild(beta);
        alphaB.AddChild(beta);

        // Inject a Smart Join Condition: A.val == B.val
        beta.JoinConditionEvaluator = (left, right) => 
        {
            var leftVal = left.Facts.FirstOrDefault(f => f.Name == "A.val")?.Value;
            var rightVal = right.Facts.FirstOrDefault(f => f.Name == "B.val")?.Value;
            
            bool result = leftVal != null && rightVal != null && leftVal.Equals(rightVal);
            Console.WriteLine($"JoinConditionEvaluator: Left A.val={leftVal ?? "null"}, Right B.val={rightVal ?? "null"} -> {result}");
            return result;
        };

        int propagations = 0;
        var terminal = new TerminalNode("TestRule", (t, s) => { 
            Console.WriteLine($"Propagated!");
            propagations++; 
        });
        beta.AddChild(terminal);

        // 2. Act
        // Assert 10 facts for A and 10 facts for B
        for(int i = 0; i < 10; i++)
        {
            network.Root.AssertFact(new Fact("A", new Dictionary<string, object> { { "id", $"A{i}" }, { "val", i } }), session);
        }

        for(int i = 0; i < 10; i++)
        {
            // Only B with val = 5 will match A with val = 5
            network.Root.AssertFact(new Fact("B", new Dictionary<string, object> { { "id", $"B{i}" }, { "val", i == 5 ? 5 : -1 } }), session);
        }

        while (network.FireNext(session)) { }

        // 3. Assert
        // Without Smart Join, a naive Cartesian product would produce 10 x 10 = 100 propagations!
        // With Smart Join, it should only propagate exactly the 1 match (A5 joined with B5).
        Assert.Equal(1, propagations);
    }
}
