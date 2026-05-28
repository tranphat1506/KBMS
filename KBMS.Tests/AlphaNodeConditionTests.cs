using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Reasoning;
using KBMS.Reasoning.Rete;
using KBMS.Models;
using Xunit;

namespace KBMS.Tests;

public class AlphaNodeConditionTests
{
    [Fact]
    public void AlphaNode_ShouldFilterTokensByCondition()
    {
        // 1. Arrange
        var network = new ReteNetwork();
        var session = new InferenceSession();
        var engine = new InferenceEngine();
        
        string conditionExpression = "age > 18";
        Func<Token, bool> conditionFunc = token => 
        {
            var facts = token.ToDictionary();
            return engine.EvaluateConstraint(conditionExpression, facts);
        };

        var alphaNode = network.GetOrCreateAlphaNode("age", conditionExpression, conditionFunc);

        // Track propagations
        int propagations = 0;
        var mockTerminal = new TerminalNode("TestRule", (t, s) => { propagations++; });
        alphaNode.AddChild(mockTerminal);

        // 2. Act
        // Invalid fact (should be filtered)
        network.AssertFact("age", 15, session);
        
        // Valid fact (should propagate)
        network.AssertFact("age", 25, session);
        
        while (network.FireNext(session)) { }

        // 3. Assert
        Assert.Equal(1, propagations); // Only the valid fact propagated!
        var alphaMemory = session.GetNodeMemory(alphaNode.Id);
        Assert.Single(alphaMemory);
        Assert.Equal(25, alphaMemory.First().Facts.Last().Value);
    }
}
