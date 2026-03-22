using KBMS.Models;
using KBMS.Reasoning;
using KBMS.Knowledge;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace KBMS.Tests
{
    public class TrueTypingTests
    {
        [Fact]
        public void InferenceEngine_ShouldPreserveDecimalPrecision()
        {
            var engine = new InferenceEngine();
            var concept = new Concept
            {
                Name = "Accounting",
                Variables = new List<Variable>
                {
                    new Variable { Name = "Base", Type = "DECIMAL", Scale = 2 },
                    new Variable { Name = "TaxRate", Type = "DECIMAL", Scale = 4 },
                    new Variable { Name = "Total", Type = "DECIMAL", Scale = 2 }
                },
                ConceptRules = new List<ConceptRule>
                {
                    new ConceptRule
                    {
                        Kind = "Finance",
                        Hypothesis = new List<string> { "Base > 0" },
                        Conclusion = new List<string> { "Total = Base * (1 + TaxRate)" }
                    }
                }
            };

            // Using Decimal values
            var facts = new Dictionary<string, object>
            {
                { "Base", 100.00m },
                { "TaxRate", 0.0825m }
            };

            var result = engine.FindClosure(concept, facts, new List<string> { "Total" });

            Assert.True(result.Success);
            Assert.IsType<decimal>(result.DerivedFacts["Total"]);
            // 100 * 1.0825 = 108.25
            Assert.Equal(108.25m, result.DerivedFacts["Total"]);
        }

        [Fact]
        public void InferenceEngine_ShouldHandleIntPromotionToLong()
        {
            var engine = new InferenceEngine();
            var concept = new Concept
            {
                Name = "Inventory",
                Variables = new List<Variable>
                {
                    new Variable { Name = "Stock", Type = "INT" },
                    new Variable { Name = "NewArrival", Type = "INT" },
                    new Variable { Name = "TotalStock", Type = "INT" }
                },
                ConceptRules = new List<ConceptRule>
                {
                    new ConceptRule
                    {
                        Kind = "Logistics",
                        Hypothesis = new List<string> { "Stock >= 0" },
                        Conclusion = new List<string> { "TotalStock = Stock + NewArrival" }
                    }
                }
            };

            var facts = new Dictionary<string, object>
            {
                { "Stock", 1000L },
                { "NewArrival", 500L }
            };

            var result = engine.FindClosure(concept, facts, new List<string> { "TotalStock" });

            Assert.True(result.Success);
            Assert.IsType<long>(result.DerivedFacts["TotalStock"]);
            Assert.Equal(1500L, result.DerivedFacts["TotalStock"]);
        }

        [Fact]
        public void InferenceEngine_ShouldDowngradeToDouble_WhenFloatingPointIsPresent()
        {
            var engine = new InferenceEngine();
            var concept = new Concept
            {
                Name = "Physics",
                Variables = new List<Variable>
                {
                    new Variable { Name = "Mass", Type = "DECIMAL" },
                    new Variable { Name = "Acceleration", Type = "DOUBLE" },
                    new Variable { Name = "Force", Type = "DOUBLE" }
                },
                ConceptRules = new List<ConceptRule>
                {
                    new ConceptRule
                    {
                        Kind = "Newton",
                        Hypothesis = new List<string> { "Mass > 0" },
                        Conclusion = new List<string> { "Force = Mass * Acceleration" }
                    }
                }
            };

            var facts = new Dictionary<string, object>
            {
                { "Mass", 10m },
                { "Acceleration", 9.81 } // Double
            };

            var result = engine.FindClosure(concept, facts, new List<string> { "Force" });

            Assert.True(result.Success);
            Assert.IsType<double>(result.DerivedFacts["Force"]);
            Assert.Equal(98.1, (double)result.DerivedFacts["Force"], 5);
        }
        [Fact]
        public void KnowledgeManager_ShouldEnforceTypesOnInsert()
        {
            var storage = new KBMS.Storage.StorageEngine("/tmp/kbms_test_typing", "test_key");
            var km = new KnowledgeManager(storage);
            var kbName = "TestTypingKB";
            var user = new User { Username = "root", Role = UserRole.ROOT };
            storage.CreateKb(kbName, System.Guid.NewGuid());

            var concept = new Concept
            {
                Name = "Product",
                Variables = new List<Variable>
                {
                    new Variable { Name = "Price", Type = "DECIMAL", Scale = 2 },
                    new Variable { Name = "Qty", Type = "INT" }
                }
            };
            storage.CreateConcept(kbName, concept);

            // 1. USE KB
            var useAst = new KBMS.Parser.Parser("USE " + kbName + ";").ParseAll().First();
            km.Execute(useAst, user, null);

            // 2. INSERT via query string - use correct KBQL 'ATTRIBUTE' syntax
            var insertAst = new KBMS.Parser.Parser("INSERT INTO Product ATTRIBUTE (Price: 19.995, Qty: 10.5);").ParseAll().First();
            km.Execute(insertAst, user, kbName);
            
            var objects = storage.SelectObjects(kbName).Where(o => o.ConceptName == "Product").ToList();
            Assert.Single(objects);
            
            var obj = objects[0];
            // Price: 19.995 -> 20.00 (decimal, scale 2)
            Assert.IsType<decimal>(obj.Values["Price"]);
            Assert.Equal(20.00m, obj.Values["Price"]);
            
            // Qty: 10.5 -> 10 (long)
            Assert.IsType<long>(obj.Values["Qty"]);
            Assert.Equal(10L, obj.Values["Qty"]);

            System.IO.Directory.Delete("/tmp/kbms_test_typing", true);
        }
    }
}
