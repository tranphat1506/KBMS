using Xunit;
using KBMS.Knowledge.V3.Optimizer;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast;
using System.Collections.Generic;
using KBMS.Models.V3;

namespace KBMS.Tests;

public class SystemV3Tests
{
    [Fact]
    public void KBMSException_GeneratesCorrectJson()
    {
        var ex = new KBMSException(
            ErrorStage.OPTIMIZER, 
            "Unknown concept 'foo'", 
            "SELECT * FROM foo", 
            2, 
            15
        );

        string json = ex.ToClientResponse();

        Assert.Contains("\"stage\": \"OPTIMIZER\"", json);
        Assert.Contains("Unknown concept", json);
        Assert.Contains("foo", json);
        Assert.Contains("\"line\": 2", json);
        Assert.Contains("\"column\": 15", json);
        Assert.Contains("\"snippet\": \"SELECT * FROM foo\"", json);
    }

    [Fact]
    public void QueryOptimizer_GeneratesHashJoinPhysicalPlan()
    {
        // Set up the Logical AST representing a JOIN query
        var ast = new SelectNode
        {
            ConceptName = "Student",
            Joins = new List<JoinClause>
            {
                new JoinClause 
                { 
                    Target = "Exam", 
                    OnCondition = new Condition { Field = "id", Operator = "=", Value = "student_id" }
                }
            }
        };

        var cbo = new QueryOptimizer(null!, (kb, concept) => new List<int> { 1, 2, 3 }); // Mock resolver
        var plan = cbo.ExplainSelect(ast, "TestKB");

        // The Optimizer should have chosen Hash Join as the best physical path
        Assert.NotNull(plan);
        var explainText = plan.FormatExplain();
        
        Assert.Equal("Hash Join", plan.Operation); // The root operator
        Assert.Equal(2, plan.Children.Count);
        Assert.Equal("Sequential Scan", plan.Children[0].Operation); // Probe side
        Assert.Equal("Sequential Scan", plan.Children[1].Operation); // Build side
        
        // Assert the EXPLAIN text formatter outputs our Cost-based diagnostics
        Assert.Contains("-> Hash Join", explainText);
        Assert.Contains("-> Sequential Scan (Concept: Exam)", explainText);
        Assert.Contains("-> Sequential Scan (Concept: Student)", explainText);
        Assert.Contains("Cost:", explainText);
    }
}
