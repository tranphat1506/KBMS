using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;

using System;
using System.Collections.Generic;
using KBMS.Parser;
using KBMS.Parser.Ast;
using KBMS.Models;
using KBMS.Knowledge;
using KBMS.Reasoning;
using KBMS.Storage;
using Xunit;

namespace KBMS.Tests
{
    public class Phase19Tests
    {
        [Fact]
        public void Parser_ConstraintLineColumn_ShouldWork()
        {
            var parser = new KBMS.Parser.Parser("CREATE CONCEPT T1\nCONSTRAINTS\n  c1: x > 0,\n  y < 10");
            var node = (CreateConceptNode)parser.Parse();

            Assert.Equal(2, node.Constraints.Count);
            
            // c1: x > 0
            Assert.Equal("c1", node.Constraints[0].Name);
            Assert.Equal("x>0", node.Constraints[0].Expression);
            Assert.Equal(3, node.Constraints[0].Line);
            Assert.Equal(7, node.Constraints[0].Column); // 'x' starts at col 7 (2 spaces + c1 + : + space)

            // y < 10
            Assert.Equal("", node.Constraints[1].Name);
            Assert.Equal("y<10", node.Constraints[1].Expression);
            Assert.Equal(4, node.Constraints[1].Line);
            Assert.Equal(3, node.Constraints[1].Column);
        }

        [Fact]
        public void InferenceEngine_ConstraintViolation_ShouldIncludeMetadata()
        {
            var engine = new InferenceEngine();
            var concept = new Concept
            {
                Name = "Test",
                Constraints = new List<Constraint>
                {
                    new Constraint { Name = "ValidAge", Expression = "age >= 18", Line = 5, Column = 10 }
                }
            };
            
            var facts = new Dictionary<string, object> { { "age", 15.0 } };
            var result = engine.FindClosure(concept, facts, new List<string>());

            Assert.False(result.Success);
            Assert.Contains("ValidAge", result.ErrorMessage);
            Assert.Contains("line 5", result.ErrorMessage);
            Assert.Contains("col 10", result.ErrorMessage);
        }

        [Fact]
        public void ResponseParser_DisplayError_WithPointer_ShouldWork()
        {
            var sw = new System.IO.StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);
            
            try 
            {
                var error = new KBMS.Network.ErrorResponse
                {
                    Type = "ParserError",
                    Message = "Unexpected token",
                    Query = "CREATE CONCEPT ;\nDROP CONCEPT C1",
                    Line = 1,
                    Column = 16
                };
                var json = System.Text.Json.JsonSerializer.Serialize(error);
                
                KBMS.CLI.ResponseParser.DisplayError(json);
                
                var output = sw.ToString();
                Assert.Contains("ERROR: Unexpected token", output);
                Assert.Contains("1 | CREATE CONCEPT ;", output);
                Assert.Contains("^", output); // Pointer
                Assert.Contains("(Line: 1, Column: 16)", output);
            }
            finally 
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
