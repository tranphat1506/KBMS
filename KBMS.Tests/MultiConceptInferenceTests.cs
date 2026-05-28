using Xunit;
using Xunit.Abstractions;
using KBMS.Models;
using KBMS.Knowledge;
using KBMS.Storage;
using KBMS.Parser.Ast.Kml;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace KBMS.Tests;

public class MultiConceptInferenceTests
{
    private readonly ITestOutputHelper _output;

    public MultiConceptInferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void MultiConceptRule_ShouldTriggerLazyLoadingAndInfer()
    {
        // 1. Setup Storage
        string testDir = Path.Combine(Path.GetTempPath(), "KBMS_Test_MCR_" + Guid.NewGuid().ToString("N"));
        if (!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);
        
        var pool = new KBMS.Storage.Core.StoragePool(testDir, 64);
        var kbCatalog = new KBMS.Storage.Core.KbCatalog(pool);
        var conceptCatalog = new KBMS.Storage.Core.ConceptCatalog(pool);
        var userCatalog = new KBMS.Storage.Core.UserCatalog(pool);

        var router = new KBMS.Knowledge.Core.StorageRouter(pool);
        var km = new KnowledgeManager(pool, kbCatalog, conceptCatalog, userCatalog, router);

        string kbName = "LazyLoadKB";
        kbCatalog.CreateKb(kbName, Guid.NewGuid());

        // 2. Setup Concepts: Customer and Order
        var customerConcept = new Concept { Name = "Customer" };
        customerConcept.Variables.Add(new Variable { Name = "customerId", Type = "STRING" });
        customerConcept.Variables.Add(new Variable { Name = "tier", Type = "STRING" });
        conceptCatalog.CreateConcept(kbName, customerConcept);

        var orderConcept = new Concept { Name = "Order" };
        orderConcept.Variables.Add(new Variable { Name = "orderId", Type = "STRING" });
        orderConcept.Variables.Add(new Variable { Name = "customerId", Type = "STRING" });
        orderConcept.Variables.Add(new Variable { Name = "amount", Type = "DECIMAL" });
        conceptCatalog.CreateConcept(kbName, orderConcept);

        // 3. Setup Multi-concept Rule
        // SCOPE Customer c JOIN Order o ON c.customerId = o.customerId
        // IF o.amount > 1000 THEN c.tier = "VIP"
        var rule = new Rule
        {
            Id = Guid.NewGuid(),
            Name = "VipCustomerRule",
            Scope = "Customer",
            ScopeConcepts = new List<RuleScopeConcept>
            {
                new RuleScopeConcept { ConceptName = "Customer", Alias = "c", Position = 0 },
                new RuleScopeConcept { ConceptName = "Order", Alias = "o", Position = 1 }
            },
            JoinConditions = new List<RuleJoinCondition>
            {
                new RuleJoinCondition { LeftField = "c.customerId", Operator = "=", RightField = "o.customerId" }
            },
            Hypothesis = new List<Expression> { new Expression { Content = "o.amount > 1000" } },
            Conclusion = new List<Expression> { new Expression { Content = "c.tier = \"VIP\"" } }
        };

        var kb = kbCatalog.LoadKb(kbName)!;
        kb.Rules.Add(rule);
        kbCatalog.SaveKbMetadata(kb);

        // We also attach it to Order so that inserting an Order triggers it.
        // Wait, KBMS write-time inference runs on the inserted object. If we insert an Order, 
        // KnowledgeManager uses GetConfiguredEngine which loads all rules where the object's Concept is in the scope hierarchy!
        // So the rule is loaded!

        // 4. Insert base object (Customer)
        var customerIdStr = "CUST123";
        var custObj = new ObjectInstance {
            Id = Guid.NewGuid(),
            ConceptName = "Customer",
            Values = new Dictionary<string, object> { { "customerId", customerIdStr }, { "tier", "Regular" } }
        };
        router.InsertObject(kbName, custObj);

        var getEngineMethod = km.GetType().GetMethod("GetConfiguredEngine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var engine = (KBMS.Reasoning.InferenceEngine)getEngineMethod!.Invoke(km, new object[] { kbName })!;

        var listRulesMethod = km.GetType().GetMethod("ListRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allRules = (List<Rule>)listRulesMethod!.Invoke(km, new object[] { kbName })!;
        _output.WriteLine($"Total Rules in KB: {allRules.Count}");
        if (allRules.Count > 0) {
            var r = allRules[0];
            _output.WriteLine($"Rule 0: Scope={r.Scope}, ScopeConcepts={r.ScopeConcepts?.Count}");
            if (r.ScopeConcepts != null) {
                foreach (var sc in r.ScopeConcepts) _output.WriteLine($"  SC: {sc.ConceptName}");
            }
        }

        var resolvedOrder = engine.ConceptResolver!("Order");
        _output.WriteLine($"Resolved Order rules count: {resolvedOrder?.ConceptRules.Count}");

        var inferenceValues = new Dictionary<string, object> {
            { "orderId", "ORD1" },
            { "customerId", customerIdStr },
            { "amount", 2500m }
        };
        var result = engine.FindClosure(orderConcept, inferenceValues, new List<string>());
        _output.WriteLine($"Direct FindClosure Result: Success={result.Success}, DerivedFacts={result.DerivedFacts.Count}");
        foreach (var step in result.Steps) { _output.WriteLine($"Step: {step}"); }

        var insertNode = new InsertNode {
            ConceptName = "Order",
            Values = new Dictionary<string, ValueNode> {
                { "orderId", new ValueNode { Value = "ORD1", ValueType = "STRING" } },
                { "customerId", new ValueNode { Value = customerIdStr, ValueType = "STRING" } },
                { "amount", new ValueNode { Value = 2500m, ValueType = "NUMBER" } }
            }
        };

        var handleInsertMethod = km.GetType().GetMethod("HandleInsert", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handleInsertMethod!.Invoke(km, new object[] { insertNode, kbName, new User { Role = UserRole.ROOT } });

        // 6. Verify Lazy Loading
        // The inference engine should have evaluated Order, reached BetaNode, Lazy-Loaded Customer from DB,
        // passed the filter (amount > 1000), and executed conclusion: c.tier = "VIP"
        // Since c.tier is deduced, KBMS writes it back! 
        // Ah! We have implemented Multi-concept Write-Time inference fixes!
        // The inference engine deduces c.tier = "VIP"
        Assert.True(result.DerivedFacts.ContainsKey("c.tier"));
        Assert.Equal("VIP", result.DerivedFacts["c.tier"]?.ToString());
        
        // Assert it doesn't break Order object insertion
        var orderInstances = router.SelectObjects(kbName, "Order");
        Assert.Single(orderInstances);
        var insertedOrder = orderInstances.First();
        Assert.False(insertedOrder.Values.ContainsKey("c.tier"));

        // Cleanup
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }
}
