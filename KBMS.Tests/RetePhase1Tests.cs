using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Reasoning;
using KBMS.Reasoning.Rete;
using Xunit;

namespace KBMS.Tests;

public class RetePhase1Tests
{
    [Fact]
    public void RetractFact_ShouldClearTokensAndAgenda()
    {
        var network = new ReteNetwork();
        var session = new InferenceSession();

        var alphaA = network.GetOrCreateAlphaNode("a");
        var alphaB = network.GetOrCreateAlphaNode("b");
        
        var beta = new BetaNode();
        beta.LeftParent = alphaA;
        beta.RightParent = alphaB;
        alphaA.AddChild(beta);
        alphaB.AddChild(beta);

        var terminal = new TerminalNode("rule1", (t, s) => { });
        beta.AddChild(terminal);

        network.AssertFact("a", 1, session);
        network.AssertFact("b", 2, session);

        // BetaNode should have joined them (check session memories)
        Assert.Single(session.GetBetaLeftMemory(beta.Id));
        Assert.Single(session.GetBetaRightMemory(beta.Id));

        // Retract A
        network.RetractFact("a", session);

        // Alpha 'a' fact removed, Beta memory for 'a' should be cleared
        Assert.Empty(session.GetBetaLeftMemory(beta.Id));
        // Right memory 'b' should remain
        Assert.Single(session.GetBetaRightMemory(beta.Id));
    }

    [Fact]
    public void SameVariables_ShouldAssertAndRetractSynchronously()
    {
        var network = new ReteNetwork();
        var session = new InferenceSession();

        network.ContextConcept = new Concept
        {
            SameVariables = new List<SameVariable>
            {
                new SameVariable { Variable1 = "x", Variable2 = "y" }
            }
        };

        network.AssertFact("x", 100, session);

        // Both x and y should be in working memory
        Assert.Contains(session.WorkingMemory, f => f.Name == "x" && Convert.ToInt32(f.Value) == 100);
        Assert.Contains(session.WorkingMemory, f => f.Name == "y" && Convert.ToInt32(f.Value) == 100);

        // Retract x should also retract y
        network.RetractFact("x", session);
        
        Assert.DoesNotContain(session.WorkingMemory, f => f.Name == "x");
        Assert.DoesNotContain(session.WorkingMemory, f => f.Name == "y");
    }

    [Fact]
    public void InferenceEngine_ConstructRelation_ShouldInjectEquations()
    {
        var engine = new InferenceEngine();
        
        var concept = new Concept { Name = "TestConcept" };
        concept.Variables.Add(new Variable { Name = "p1", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "p2", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "result", Type = "INT" });
        concept.ConstructRelations.Add(new ConstructRelation
        {
            RelationName = "AddRelation",
            Arguments = new List<string> { "p1", "p2", "result" }
        });

        var relation = new Relation
        {
            Name = "AddRelation",
            ParamNames = new List<string> { "a", "b", "c" },
            Equations = new List<Equation> { new Equation { Expression = "c = a + b" } }
        };

        engine.RelationResolver = name => name == "AddRelation" ? relation : null;

        var facts = new Dictionary<string, object>
        {
            { "p1", 5 },
            { "p2", 10 }
        };

        var result = engine.FindClosure(concept, facts, new List<string>());

        // 'c = a + b' mapped to 'result = p1 + p2' => 5 + 10 = 15
        Assert.True(result.Success);
        Assert.True(result.DerivedFacts.ContainsKey("result"));
        Assert.Equal(15.0, Convert.ToDouble(result.DerivedFacts["result"]));
    }
}
