using System;
using System.Collections.Generic;
using KBMS.Models;
using KBMS.Reasoning;
using Xunit;

namespace KBMS.Tests
{
    public class TriangleReasoningTests
    {
        [Fact]
        public void SolveTriangleArea_WithConstraints_ShouldSucceed()
        {
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
                    new Equation { Expression = "area = sqrt(perimeter/2 * (perimeter/2 - a) * (perimeter/2 - b) * (perimeter/2 - c))" }
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

            var result = engine.FindClosure(triangle, initialFacts, new List<string> { "area" });

            Assert.True(result.Success, $"Reasoning failed: {result.ErrorMessage}");
            Assert.True(result.DerivedFacts.ContainsKey("area"), "Area not derived");
            
            double area = Convert.ToDouble(result.DerivedFacts["area"]);
            Assert.Equal(6.0, area, 2);
            
            Assert.True(result.DerivedFacts.ContainsKey("perimeter"), "Perimeter not derived");
            Assert.Equal(12.0, Convert.ToDouble(result.DerivedFacts["perimeter"]), 2);
        }
    }
}
