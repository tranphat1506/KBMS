using System;
using KBMS.Reasoning.Rete;
using KBMS.Models;
using Xunit;

namespace KBMS.Tests;

public class AgendaTests
{
    [Fact]
    public void Agenda_ShouldPrioritizeLowestCostThenHighestPriority()
    {
        var agenda = new Agenda();
        var network = new ReteNetwork();
        
        var t1 = new TerminalNode("RuleHighCost", (t, s) => { }, cost: 10, priority: 100);
        var t2 = new TerminalNode("RuleLowCost", (t, s) => { }, cost: 2, priority: 10);
        var t3 = new TerminalNode("RuleEqualCostLowPri", (t, s) => { }, cost: 2, priority: 5);
        var t4 = new TerminalNode("RuleEqualCostHighPri", (t, s) => { }, cost: 2, priority: 50);

        var token = new Token();
        
        agenda.AddActivation(new Activation(t1, token, t1.Cost, t1.Priority));
        agenda.AddActivation(new Activation(t2, token, t2.Cost, t2.Priority));
        agenda.AddActivation(new Activation(t3, token, t3.Cost, t3.Priority));
        agenda.AddActivation(new Activation(t4, token, t4.Cost, t4.Priority));

        Assert.Equal(4, agenda.Count);

        // Expected order:
        // 1. Cost 2, Pri 50 (t4)
        // 2. Cost 2, Pri 10 (t2)
        // 3. Cost 2, Pri 5  (t3)
        // 4. Cost 10, Pri 100 (t1)

        Assert.Equal("RuleEqualCostHighPri", agenda.PopNext()?.Node.RuleName);
        Assert.Equal("RuleLowCost", agenda.PopNext()?.Node.RuleName);
        Assert.Equal("RuleEqualCostLowPri", agenda.PopNext()?.Node.RuleName);
        Assert.Equal("RuleHighCost", agenda.PopNext()?.Node.RuleName);
        Assert.Null(agenda.PopNext());
    }

    [Fact]
    public void Agenda_ShouldThrowExceptionOnMaxFireLimit()
    {
        var agenda = new Agenda();
        agenda.MaxFireLimit = 5; // Very small limit for testing
        
        var network = new ReteNetwork();
        var terminal = new TerminalNode("InfiniteRule", (t, s) => { });
        var token = new Token();

        // Simulate an infinite rule pushing activations constantly
        for (int i = 0; i < 5; i++)
        {
            agenda.AddActivation(new Activation(terminal, new Token(new[] { new Fact("dummy", i) }), 1, 50));
            agenda.PopNext(); // This fires and increments _fireCount
        }

        // The 6th pop should trigger the Rule Avalanche exception
        agenda.AddActivation(new Activation(terminal, new Token(new[] { new Fact("dummy", 6) }), 1, 50));
        
        var ex = Assert.Throws<Exception>(() => agenda.PopNext());
        Assert.Contains("Rule Avalanche Detected", ex.Message);
    }

    [Fact]
    public void Agenda_ShouldNotAddDuplicateActivations()
    {
        var agenda = new Agenda();
        var network = new ReteNetwork();
        var terminal = new TerminalNode("DuplicateRule", (t, s) => { });
        
        var fact = new Fact("sensor_temp", 45);
        var token1 = new Token(new[] { fact });
        var token2 = new Token(new[] { new Fact("sensor_temp", 45) }); // Exact same facts

        agenda.AddActivation(new Activation(terminal, token1, 1, 1));
        agenda.AddActivation(new Activation(terminal, token2, 1, 1)); // Should be ignored

        Assert.Equal(1, agenda.Count);
    }

    [Fact]
    public void Agenda_ShouldRemoveActivationsForRetractedFact()
    {
        var agenda = new Agenda();
        var network = new ReteNetwork();
        var terminal = new TerminalNode("RetractRule", (t, s) => { });
        
        var token1 = new Token(new[] { new Fact("a", 1), new Fact("b", 2) });
        var token2 = new Token(new[] { new Fact("c", 3) });

        agenda.AddActivation(new Activation(terminal, token1, 1, 1));
        agenda.AddActivation(new Activation(terminal, token2, 1, 1));

        Assert.Equal(2, agenda.Count);

        agenda.RemoveActivationsForFact("b");

        Assert.Equal(1, agenda.Count);
        var remaining = agenda.PopNext();
        Assert.Equal(3, remaining?.Token.GetValue("c"));
    }
}
