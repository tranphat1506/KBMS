using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Reasoning;
using Xunit;
using Xunit.Abstractions;

namespace KBMS.Tests;

public class ReteCoordinationTests
{
    private readonly ITestOutputHelper _output;

    public ReteCoordinationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Rete_FullCoordination_CHR_Rule_Ops_ShouldWork()
    {
        // 1. Setup Environment
        var engine = new InferenceEngine();
        
        // 2. Define Hierarchy (H) - ElectronicComponent -> Resistor
        var compConcept = new Concept
        {
            Name = "ElectronicComponent",
            Variables = new List<Variable> {
                new Variable { Name = "status", Type = "STRING" },
                new Variable { Name = "color", Type = "STRING" }
            },
            ConceptRules = new List<ConceptRule> {
                new ConceptRule {
                    Kind = "InheritedDamageRule",
                    Hypothesis = new List<string> { "status = 'Damaged'" },
                    Conclusion = new List<string> { "SET color = 'Black'" }
                }
            }
        };

        var resistorConcept = new Concept
        {
            Name = "Resistor",
            BaseObjects = new List<string> { "ElectronicComponent" },
            Variables = new List<Variable> {
                new Variable { Name = "u", Type = "DECIMAL" },
                new Variable { Name = "i", Type = "DECIMAL" },
                new Variable { Name = "r", Type = "DECIMAL" }
            },
            Equations = new List<Equation> {
                new Equation { Expression = "u = i * r" }
            }
        };

        // 3. Define Relation (R) - SeriesLink
        var seriesRel = new Relation
        {
            Name = "SeriesLink",
            ParamNames = new List<string> { "c1", "c2", "total" },
            Equations = new List<Equation> {
                new Equation { Expression = "total.r = c1.r + c2.r" }
            }
        };

        // 4. Define Circuit Concept (C)
        var circuitConcept = new Concept
        {
            Name = "Circuit",
            Variables = new List<Variable> {
                new Variable { Name = "r1", Type = "Resistor" },
                new Variable { Name = "r2", Type = "Resistor" },
                new Variable { Name = "total_r", Type = "DECIMAL" }
            },
            ConstructRelations = new List<ConstructRelation> {
                new ConstructRelation {
                    RelationName = "SeriesLink",
                    Arguments = new List<string> { "r1", "r2", "this" } // 'this' maps to total_r in context
                }
            },
            SameVariables = new List<SameVariable> {
                new SameVariable { Variable1 = "total_r", Variable2 = "this.r" }
            }
        };

        // 5. Setup Resolvers
        engine.ConceptResolver = name => {
            if (name == "ElectronicComponent") return compConcept;
            if (name == "Resistor") return resistorConcept;
            if (name == "Circuit") return circuitConcept;
            return null;
        };
        engine.HierarchyResolver = name => name == "Resistor" ? new List<string> { "ElectronicComponent" } : new List<string>();
        engine.RelationResolver = name => name == "SeriesLink" ? seriesRel : null;

        // 6. Execute Test
        var initialFacts = new Dictionary<string, object>
        {
            { "r1.i", 2.0 },
            { "r1.r", 10.0 },
            { "r2.i", 1.0 },
            { "r2.r", 20.0 },
            { "r1.status", "Damaged" }
        };

        _output.WriteLine("Starting Coordinated Rete Test...");
        var result = engine.FindClosure(circuitConcept, initialFacts, new List<string>());

        foreach (var step in result.Steps)
        {
            _output.WriteLine(step);
        }

        // 7. Assertions
        _output.WriteLine("\n--- Final Facts ---");
        foreach (var fact in result.DerivedFacts)
        {
            _output.WriteLine($"{fact.Key} = {fact.Value}");
        }

        // Check Ops (Ohm's Law)
        Assert.True(result.DerivedFacts.ContainsKey("r1.u"), "r1.u should be calculated");
        Assert.Equal(20.0, Convert.ToDouble(result.DerivedFacts["r1.u"]));
        
        Assert.True(result.DerivedFacts.ContainsKey("r2.u"), "r2.u should be calculated");
        Assert.Equal(20.0, Convert.ToDouble(result.DerivedFacts["r2.u"]));

        // Check Relation (R) - Series total
        Assert.True(result.DerivedFacts.ContainsKey("total_r"), "total_r should be calculated from relation context");
        Assert.Equal(30.0, Convert.ToDouble(result.DerivedFacts["total_r"]));

        // Check Hierarchy (H) + Rule
        Assert.True(result.DerivedFacts.ContainsKey("r1.color"), "r1.color should be inherited and fired");
        Assert.Equal("Black", result.DerivedFacts["r1.color"].ToString());
    }
}
