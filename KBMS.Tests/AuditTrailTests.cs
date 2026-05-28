using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Reasoning;
using KBMS.Reasoning.Rete;
using Xunit;

namespace KBMS.Tests;

public class AuditTrailTests
{
    [Fact]
    public void Token_ShouldTrackAuditTrailAndGeneratedVariables()
    {
        // 1. Arrange
        var engine = new InferenceEngine();
        var network = new ReteNetwork();
        var session = new InferenceSession();
        var compiler = new ReteCompiler(engine, network);
        var concept = new Concept { Name = "Patient" };
        concept.Variables.Add(new Variable { Name = "sys", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "is_hypertension", Type = "BOOLEAN" });

        var rule = new ConceptRule
        {
            Id = Guid.NewGuid(),
            Kind = "HypertensionRule",
            Hypothesis = new List<string> { "sys > 140" },
            Conclusion = new List<string> { "is_hypertension = true" },
            Cost = 5
        };
        concept.ConceptRules.Add(rule);

        compiler.Compile(concept);

        // 2. Act
        network.AssertFact("sys", 150, session);
        
        // Assert Agenda received the activation
        Assert.Equal(1, session.Agenda.Count);
        
        var activation = session.Agenda.PopNext();
        Assert.NotNull(activation);
        
        var terminal = activation.Node;
        var token = activation.Token;
        
        // Execute the terminal node manually to test Token Audit Trail mutation
        terminal.OnActivation(token, session);
        
        Assert.Single(token.AuditTrail);
        var step = token.AuditTrail.First();
        
        Assert.Equal("HypertensionRule", step.RuleName);
        Assert.Equal(5, step.StepCost);
        Assert.True(step.InputFacts.ContainsKey("sys"));
        Assert.Equal(150, step.InputFacts["sys"]);
        Assert.True(step.OutputFacts.ContainsKey("is_hypertension"));
        Assert.Equal(true, step.OutputFacts["is_hypertension"]);
        
        Assert.Contains("is_hypertension", token.GeneratedVariables);
    }
}
