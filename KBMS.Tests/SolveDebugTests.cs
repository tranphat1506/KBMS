using Xunit;
using Xunit.Abstractions;
using KBMS.Reasoning;
using KBMS.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KBMS.Tests
{
    public class SolveDebugTests
    {
        private readonly ITestOutputHelper _output;

        public SolveDebugTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Solve1DEquation_SimpleEquation_ShouldSolveFast()
        {
            var engine = new InferenceEngine();
            var parameters = new Dictionary<string, object>
            {
                ["a"] = 3.0,
                ["b"] = 4.0,
                ["c"] = 5.0
            };

            var sw = Stopwatch.StartNew();

            // Test: perimeter = a + b + c => solve for perimeter
            var result = engine.Solve1DEquation("perimeter = a + b + c", "perimeter", parameters);

            sw.Stop();

            _output.WriteLine($"Solve time: {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"Result: {result}");

            Assert.True(sw.ElapsedMilliseconds < 1000, $"Solve took too long: {sw.ElapsedMilliseconds}ms");
            Assert.False(double.IsNaN(result));
            Assert.Equal(12.0, result, 2);
        }

        [Fact]
        public void Solve1DEquation_HeronsFormula_ShouldSolveFast()
        {
            var engine = new InferenceEngine();
            var parameters = new Dictionary<string, object>
            {
                ["a"] = 3.0,
                ["b"] = 4.0,
                ["c"] = 5.0
            };

            var sw = Stopwatch.StartNew();

            // Heron's formula: area = Sqrt(s*(s-a)*(s-b)*(s-c)) where s = (a+b+c)/2
            // Simplified: area = Sqrt(6*3*2*1) = Sqrt(36) = 6
            var result = engine.Solve1DEquation("area = Sqrt(6*(6-a)*(6-b)*(6-c))", "area", parameters);

            sw.Stop();

            _output.WriteLine($"Solve time: {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"Result: {result}");

            Assert.True(sw.ElapsedMilliseconds < 1000, $"Solve took too long: {sw.ElapsedMilliseconds}ms");
            Assert.False(double.IsNaN(result));
            Assert.Equal(6.0, result, 2);
        }

        [Fact]
        public void FindClosure_SimpleConcept_ShouldSolveFast()
        {
            var engine = new InferenceEngine();

            var concept = new Concept
            {
                Name = "Triangle",
                Variables = new List<Variable>
                {
                    new() { Name = "a", Type = "DECIMAL" },
                    new() { Name = "b", Type = "DECIMAL" },
                    new() { Name = "c", Type = "DECIMAL" },
                    new() { Name = "perimeter", Type = "DECIMAL" }
                },
                Equations = new List<Equation>
                {
                    new() { Expression = "perimeter = a + b + c" }
                }
            };

            var facts = new Dictionary<string, object>
            {
                ["a"] = 3.0,
                ["b"] = 4.0,
                ["c"] = 5.0
            };

            var sw = Stopwatch.StartNew();
            var result = engine.FindClosure(concept, facts, new List<string> { "perimeter" });
            sw.Stop();

            _output.WriteLine($"FindClosure time: {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"Success: {result.Success}");
            _output.WriteLine($"Derived: {string.Join(", ", result.DerivedFacts)}");
            _output.WriteLine($"Steps: {string.Join("\n", result.Steps)}");

            Assert.True(sw.ElapsedMilliseconds < 5000, $"FindClosure took too long: {sw.ElapsedMilliseconds}ms");
            Assert.True(result.Success);
            Assert.True(result.DerivedFacts.ContainsKey("perimeter"));
            Assert.Equal(12.0, Convert.ToDouble(result.DerivedFacts["perimeter"]), 2);
        }

        [Fact]
        public void EvaluateFormula_SimpleMath_ShouldBeFast()
        {
            var engine = new InferenceEngine();
            var parameters = new Dictionary<string, object>
            {
                ["a"] = 3.0,
                ["b"] = 4.0,
                ["c"] = 5.0
            };

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                var result = engine.EvaluateFormula("a + b + c", parameters);
            }

            sw.Stop();

            _output.WriteLine($"100 evaluations time: {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"Per evaluation: {sw.ElapsedMilliseconds / 100.0}ms");

            Assert.True(sw.ElapsedMilliseconds < 1000, $"EvaluateFormula too slow: {sw.ElapsedMilliseconds}ms for 100 calls");
        }
    }
}
