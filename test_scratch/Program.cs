using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Reasoning;
using KBMS.Models;

var engine = new InferenceEngine();

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

engine.ConceptResolver = name => {
    if (name == "ElectronicComponent") return compConcept;
    if (name == "Resistor") return resistorConcept;
    return null;
};
engine.HierarchyResolver = name => name == "Resistor" ? new List<string> { "ElectronicComponent" } : new List<string>();

var initialFacts = new Dictionary<string, object>
{
    { "r1.status", "Damaged" }
};

var circuitConcept = new Concept {
    Name = "Circuit",
    Variables = new List<Variable> { new Variable { Name = "r1", Type = "Resistor" } }
};

var result = engine.FindClosure(circuitConcept, initialFacts, new List<string>());

Console.WriteLine($"Success: {result.Success}");
Console.WriteLine("Derived Facts:");
foreach (var fact in result.DerivedFacts)
{
    Console.WriteLine($"  {fact.Key} = {fact.Value}");
}
