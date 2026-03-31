using System;
using System.Collections.Generic;
using KBMS.Models;
using KBMS.Storage.V3;
using Xunit;

namespace KBMS.Tests
{
    public class ModelBinaryUtilityTests
    {
        [Fact]
        public void SerializeDeserialize_Concept_ShouldMatch()
        {
            var concept = new Concept
            {
                Id = Guid.NewGuid(),
                KbId = Guid.NewGuid(),
                Name = "Triangle",
                Variables = new List<Variable>
                {
                    new Variable { Name = "a", Type = "int", Length = 4 },
                    new Variable { Name = "b", Type = "int", Length = 4 },
                    new Variable { Name = "c", Type = "int", Length = 4 }
                },
                Equations = new List<Equation>
                {
                    new Equation
                    {
                        Id = Guid.NewGuid(),
                        Expression = "a+b>c",
                        Variables = new List<string> { "a", "b", "c" },
                        Line = 1, Column = 1
                    }
                }
            };

            var data = ModelBinaryUtility.SerializeConcept(concept);
            var deserialized = ModelBinaryUtility.DeserializeConcept(data);

            Assert.NotNull(deserialized);
            Assert.Equal(concept.Id, deserialized.Id);
            Assert.Equal(concept.Name, deserialized.Name);
            
            Assert.Single(deserialized.Equations);
            Assert.Equal("a+b>c", deserialized.Equations[0].Expression);
            
            Assert.Equal(3, deserialized.Variables.Count);
            Assert.Equal("b", deserialized.Variables[1].Name);
            Assert.Equal(4, deserialized.Variables[1].Length);
        }

        [Fact]
        public void SerializeDeserialize_Rule_ShouldMatch()
        {
            var rule = new Rule
            {
                Id = Guid.NewGuid(),
                KbId = Guid.NewGuid(),
                Name = "TriangleValidityRule",
                RuleType = "constraint",
                Scope = "Global",
                Cost = 10,
                Hypothesis = new List<Expression>
                {
                    new Expression
                    {
                        Type = "BinaryOp",
                        Content = ">",
                        Children = new List<Expression>
                        {
                            new Expression { Type = "Var", Content = "a" },
                            new Expression { Type = "Val", Content = "0" }
                        }
                    }
                }
            };

            var data = ModelBinaryUtility.SerializeRule(rule);
            var deserialized = ModelBinaryUtility.DeserializeRule(data);

            Assert.NotNull(deserialized);
            Assert.Equal(rule.Id, deserialized.Id);
            Assert.Equal(rule.RuleType, deserialized.RuleType);
            Assert.Equal(rule.Cost, deserialized.Cost);
            
            Assert.Single(deserialized.Hypothesis);
            Assert.Equal(">", deserialized.Hypothesis[0].Content);
            Assert.Equal(2, deserialized.Hypothesis[0].Children.Count);
            Assert.Equal("a", deserialized.Hypothesis[0].Children[0].Content);
        }

        [Fact]
        public void SerializeDeserialize_Hierarchy_ShouldMatch()
        {
            var hierarchy = new Hierarchy
            {
                Id = Guid.NewGuid(),
                KbId = Guid.NewGuid(),
                ParentConcept = "Polygon",
                ChildConcept = "Triangle",
                HierarchyType = HierarchyType.IsA
            };

            var data = ModelBinaryUtility.SerializeHierarchy(hierarchy);
            var deserialized = ModelBinaryUtility.DeserializeHierarchy(data);

            Assert.NotNull(deserialized);
            Assert.Equal(hierarchy.ParentConcept, deserialized.ParentConcept);
            Assert.Equal(hierarchy.ChildConcept, deserialized.ChildConcept);
            Assert.Equal(hierarchy.HierarchyType, deserialized.HierarchyType);
        }

        [Fact]
        public void SerializeDeserialize_Function_ShouldMatch()
        {
            var function = new Function
            {
                Id = Guid.NewGuid(),
                Name = "CalculateArea",
                ReturnType = "float",
                Body = "return 0.5 * b * h;",
                Parameters = new List<FunctionParameter>
                {
                    new FunctionParameter { Name = "b", Type = "float" },
                    new FunctionParameter { Name = "h", Type = "float" }
                }
            };

            var data = ModelBinaryUtility.SerializeFunction(function);
            var deserialized = ModelBinaryUtility.DeserializeFunction(data);

            Assert.NotNull(deserialized);
            Assert.Equal(function.Name, deserialized.Name);
            Assert.Equal(function.Body, deserialized.Body);
            
            Assert.Equal(2, deserialized.Parameters.Count);
            Assert.Equal("float", deserialized.Parameters[1].Type);
            Assert.Equal("h", deserialized.Parameters[1].Name);
        }
    }
}
