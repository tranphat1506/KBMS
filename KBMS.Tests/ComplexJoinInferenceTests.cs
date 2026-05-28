using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Knowledge;
using KBMS.Storage.Core;
using Xunit;
using Xunit.Abstractions;

namespace KBMS.Tests;

public class ComplexJoinInferenceTests
{
    private readonly ITestOutputHelper _output;

    public ComplexJoinInferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Rule_ShouldSupportComplexJoinOperators()
    {
        var kbName = "ComplexJoinKB";
        string testDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KBMS_Test_CJ_" + Guid.NewGuid().ToString("N"));
        if (!System.IO.Directory.Exists(testDir)) System.IO.Directory.CreateDirectory(testDir);
        
        var pool = new StoragePool(testDir, 64);
        var kbCatalog = new KbCatalog(pool);
        var conceptCatalog = new ConceptCatalog(pool);
        var userCatalog = new UserCatalog(pool);
        var router = new KBMS.Knowledge.Core.StorageRouter(pool);
        var km = new KnowledgeManager(pool, kbCatalog, conceptCatalog, userCatalog, router);

        try
        {
            kbCatalog.CreateKb(kbName, Guid.NewGuid());

            // 1. Create Patient Concept
            var patientConcept = new Concept { Name = "Patient" };
            patientConcept.Variables.Add(new Variable { Name = "patientId", Type = "STRING" });
            patientConcept.Variables.Add(new Variable { Name = "threshold", Type = "NUMBER" });
            patientConcept.Variables.Add(new Variable { Name = "riskLevel", Type = "STRING" });
            conceptCatalog.CreateConcept(kbName, patientConcept);

            // 2. Create Vitals Concept
            var vitalsConcept = new Concept { Name = "Vitals" };
            vitalsConcept.Variables.Add(new Variable { Name = "patientId", Type = "STRING" });
            vitalsConcept.Variables.Add(new Variable { Name = "sys", Type = "NUMBER" });
            vitalsConcept.Variables.Add(new Variable { Name = "dia", Type = "NUMBER" });
            conceptCatalog.CreateConcept(kbName, vitalsConcept);

            // 3. Define Rule with complex joins:
            var rule = new Rule
            {
                Id = Guid.NewGuid(),
                Name = "HighRiskRule",
                Scope = "Patient",
                ScopeConcepts = new List<RuleScopeConcept>
                {
                    new RuleScopeConcept { ConceptName = "Patient", Alias = "p", Position = 0 },
                    new RuleScopeConcept { ConceptName = "Vitals", Alias = "v", Position = 1 }
                },
                JoinConditions = new List<RuleJoinCondition>
                {
                    new RuleJoinCondition { LeftField = "p.patientId", Operator = "=", RightField = "v.patientId" },
                    new RuleJoinCondition { LeftField = "v.sys", Operator = ">", RightField = "p.threshold" }
                },
                Hypothesis = new List<KBMS.Models.Expression> { new KBMS.Models.Expression { Content = "v.sys > p.threshold" } },
                Conclusion = new List<KBMS.Models.Expression> { new KBMS.Models.Expression { Content = "p.riskLevel = 'High'" } }
            };

            var kb = kbCatalog.LoadKb(kbName)!;
            kb.Rules.Add(rule);
            kbCatalog.SaveKbMetadata(kb);

            // 4. Insert Patient
            var p1 = new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "Patient", Values = new Dictionary<string, object> { { "patientId", "P1" }, { "threshold", 140m }, { "riskLevel", "Normal" } } };
            router.InsertObject(kbName, p1);

            // 5. Insert Vitals (Triggers inference on Vitals)
            var getEngineMethod = km.GetType().GetMethod("GetConfiguredEngine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var engine = (KBMS.Reasoning.InferenceEngine)getEngineMethod!.Invoke(km, new object[] { kbName })!;

            var inferenceValues = new Dictionary<string, object> {
                { "patientId", "P1" },
                { "sys", 150m },
                { "dia", 90m }
            };
            var result = engine.FindClosure(vitalsConcept, inferenceValues, new List<string>());
            
            // 6. Verify Patient updated (InferenceResult should contain riskLevel)
            Assert.True(result.DerivedFacts.ContainsKey("p.riskLevel") || result.DerivedFacts.ContainsKey("riskLevel"), "The rule should have fired and derived the High risk level.");
        }
        finally
        {
            if (System.IO.Directory.Exists(testDir)) System.IO.Directory.Delete(testDir, true);
        }
    }
}
