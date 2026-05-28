using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using KBMS.Models;
using KBMS.Reasoning;
using KBMS.Reasoning.Rete;

namespace KBMS.Tests;

public class ConstructRelationInstantiationTests
{
    [Fact]
    public void InferenceEngine_ShouldFlattenSubObjectsAndRelationRules()
    {
        // 1. Arrange: Create the underlying "DoanThang" concept
        var doanThang = new Concept
        {
            Name = "DoanThang",
            Variables = new List<Variable>
            {
                new Variable { Name = "length", Type = "FLOAT" }
            },
            Equations = new List<Equation>
            {
                new Equation { Expression = "length > 0" }
            },
            ConceptRules = new List<ConceptRule>
            {
                new ConceptRule
                {
                    Kind = "RULE",
                    Hypothesis = new List<string> { "length > 10" },
                    Conclusion = new List<string> { "is_long = 1" }
                }
            }
        };

        // 2. Arrange: Create the "Pytago" relation with Equations and Rules
        var pytagoRelation = new Relation
        {
            Name = "Pytago",
            ParamNames = new List<string> { "a", "b", "c" },
            Equations = new List<Equation>
            {
                new Equation { Expression = "a.length + b.length = c.length" }
            },
            Rules = new List<ConceptRule>
            {
                new ConceptRule
                {
                    Kind = "RULE",
                    Hypothesis = new List<string> { "a.length > 0", "b.length > 0" },
                    Conclusion = new List<string> { "c.length > 0" }
                }
            }
        };

        // 3. Arrange: Create the "TamGiac" concept that uses DoanThang as variables and Pytago as ConstructRelation
        var tamGiac = new Concept
        {
            Name = "TamGiac",
            Variables = new List<Variable>
            {
                new Variable { Name = "Canh_A", Type = "DoanThang", IsReference = true },
                new Variable { Name = "Canh_B", Type = "DoanThang", IsReference = true },
                new Variable { Name = "Canh_C", Type = "DoanThang", IsReference = true }
            },
            ConstructRelations = new List<ConstructRelation>
            {
                new ConstructRelation
                {
                    RelationName = "Pytago",
                    Arguments = new List<string> { "Canh_A", "Canh_B", "Canh_C" }
                }
            }
        };

        var engine = new InferenceEngine();
        engine.ConceptResolver = (name) =>
        {
            if (name == "DoanThang") return doanThang;
            if (name == "TamGiac") return tamGiac;
            return null;
        };
        engine.RelationResolver = (name) =>
        {
            if (name == "Pytago") return pytagoRelation;
            return null;
        };

        // 4. Act: Trigger reasoning
        var knownFacts = new Dictionary<string, object>
        {
            { "Canh_A.length", 3 },
            { "Canh_B.length", 4 }
        };

        var result = engine.FindClosure(tamGiac, knownFacts, new List<string> { "Canh_C.length" });

        // 5. Assert: Check if composition mapping worked
        
        if (!result.DerivedFacts.ContainsKey("Canh_C.length"))
        {
            System.IO.File.WriteAllLines("test_debug.txt", result.Steps);
        }
        Assert.True(result.DerivedFacts.ContainsKey("Canh_C.length"));
        Assert.Equal(7.0, Math.Round(Convert.ToDouble(result.DerivedFacts["Canh_C.length"]), 4));
    }
}
