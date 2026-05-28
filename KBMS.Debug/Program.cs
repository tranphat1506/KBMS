using System;
using System.Collections.Generic;
using KBMS.Models;
using KBMS.Reasoning;

var engine = new InferenceEngine();
var triangle = new Concept
{
    Name = "Triangle",
    Variables = new List<Variable>
    {
        new Variable { Name = "a", Type = "DOUBLE" },
        new Variable { Name = "b", Type = "DOUBLE" },
        new Variable { Name = "c", Type = "DOUBLE" },
        new Variable { Name = "perimeter", Type = "DOUBLE" },
        new Variable { Name = "area", Type = "DOUBLE" }
    },
    Equations = new List<Equation>
    {
        new Equation { Expression = "perimeter = a + b + c" },
        new Equation { Expression = "area = Sqrt(perimeter/2 * (perimeter/2 - a) * (perimeter/2 - b) * (perimeter/2 - c))" }
    },
    Constraints = new List<Constraint>
    {
        new Constraint { Expression = "a + b > c" },
        new Constraint { Expression = "b + c > a" },
        new Constraint { Expression = "a + c > b" }
    }
};

var initialFacts = new Dictionary<string, object>
{
    { "a", 3.0 },
    { "b", 4.0 },
    { "c", 5.0 }
};

Console.WriteLine("Starting FindClosure...");
var result = engine.FindClosure(triangle, initialFacts, new List<string> { "area" });
Console.WriteLine($"Finished! Success={result.Success}");
