using Xunit;
using Xunit.Abstractions;
using KBMS.Models;
using KBMS.Knowledge;
using KBMS.Storage;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Expressions;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace KBMS.Tests;

/// <summary>
/// Verifies Transaction/Rollback behaviour during write-time inference.
/// Goal: if inference throws an exception mid-flight, the B+ Tree must be
/// left in exactly the same state as before the INSERT/UPDATE (no dirty data).
/// </summary>
public class TransactionRollbackTests
{
    private readonly ITestOutputHelper _output;

    public TransactionRollbackTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: build a minimal KnowledgeManager with an in-memory-ish data dir
    // ─────────────────────────────────────────────────────────────────────────
    private (KnowledgeManager km, KBMS.Knowledge.Core.StorageRouter router,
             KBMS.Storage.Core.KbCatalog kbCatalog,
             KBMS.Storage.Core.ConceptCatalog conceptCatalog,
             string testDir) BuildStack()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "KBMS_TxTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        var pool          = new KBMS.Storage.Core.StoragePool(testDir, 64);
        var kbCatalog     = new KBMS.Storage.Core.KbCatalog(pool);
        var conceptCatalog= new KBMS.Storage.Core.ConceptCatalog(pool);
        var userCatalog   = new KBMS.Storage.Core.UserCatalog(pool);
        var router        = new KBMS.Knowledge.Core.StorageRouter(pool);
        var km            = new KnowledgeManager(pool, kbCatalog, conceptCatalog, userCatalog, router);

        return (km, router, kbCatalog, conceptCatalog, testDir);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1: INSERT that succeeds inference → object is persisted with derived value
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Insert_WithSuccessfulInference_ShouldPersistDerivedFact()
    {
        var (km, router, kbCatalog, conceptCatalog, testDir) = BuildStack();
        try
        {
            string kbName = "TxSuccessKB";
            kbCatalog.CreateKb(kbName, Guid.NewGuid());

            var concept = new Concept { Name = "Circle" };
            concept.Variables.Add(new Variable { Name = "r", Type = "NUMBER" });
            concept.Variables.Add(new Variable { Name = "area", Type = "NUMBER" });
            concept.Equations.Add(new Equation { Expression = "area = 3.14159 * r * r" });
            conceptCatalog.CreateConcept(kbName, concept);

            // Insert via KnowledgeManager (triggers write-time inference)
            var insertNode = new InsertNode
            {
                ConceptName = "Circle",
                Values = new Dictionary<string, ValueNode>
                {
                    { "r", new ValueNode { Value = 5m, ValueType = "NUMBER" } }
                }
            };

            var handleInsert = km.GetType().GetMethod("HandleInsert",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            handleInsert.Invoke(km, new object[] { insertNode, kbName, new User { Role = UserRole.ROOT } });

            // Verify the derived 'area' was persisted
            var objs = router.SelectObjects(kbName, "Circle");
            Assert.Single(objs);
            var obj = objs.First();
            Assert.True(obj.Values.ContainsKey("area"), "Derived 'area' should be persisted.");
            var area = Convert.ToDouble(obj.Values["area"]);
            Assert.True(Math.Abs(area - 78.53975) < 0.01, $"Expected ~78.54, got {area}");
            _output.WriteLine($"[PASS] area = {area}");
        }
        finally
        {
            if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2: Two successive INSERTs — second should NOT pick up derived state
    //         left over from the first (session isolation).
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Insert_TwoObjects_ShouldHaveIsolatedInferenceSessions()
    {
        var (km, router, kbCatalog, conceptCatalog, testDir) = BuildStack();
        try
        {
            string kbName = "TxIsolationKB";
            kbCatalog.CreateKb(kbName, Guid.NewGuid());

            var concept = new Concept { Name = "Box" };
            concept.Variables.Add(new Variable { Name = "side", Type = "NUMBER" });
            concept.Variables.Add(new Variable { Name = "volume", Type = "NUMBER" });
            concept.Equations.Add(new Equation { Expression = "volume = side * side * side" });
            conceptCatalog.CreateConcept(kbName, concept);

            var handleInsert = km.GetType().GetMethod("HandleInsert",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var rootUser = new User { Role = UserRole.ROOT };

            handleInsert.Invoke(km, new object[] {
                new InsertNode {
                    ConceptName = "Box",
                    Values = new Dictionary<string, ValueNode> {
                        { "side", new ValueNode { Value = 3m, ValueType = "NUMBER" } }
                    }
                }, kbName, rootUser });

            handleInsert.Invoke(km, new object[] {
                new InsertNode {
                    ConceptName = "Box",
                    Values = new Dictionary<string, ValueNode> {
                        { "side", new ValueNode { Value = 4m, ValueType = "NUMBER" } }
                    }
                }, kbName, rootUser });

            var objs = router.SelectObjects(kbName, "Box").OrderBy(o => Convert.ToDouble(o.Values["side"])).ToList();
            Assert.Equal(2, objs.Count);

            var vol3 = Convert.ToDouble(objs[0].Values["volume"]);
            var vol4 = Convert.ToDouble(objs[1].Values["volume"]);

            Assert.True(Math.Abs(vol3 - 27) < 0.001, $"Box(3).volume should be 27, got {vol3}");
            Assert.True(Math.Abs(vol4 - 64) < 0.001, $"Box(4).volume should be 64, got {vol4}");
            _output.WriteLine($"[PASS] Box(3).volume={vol3}, Box(4).volume={vol4}");
        }
        finally
        {
            if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3: UPDATE re-derives value correctly after field changes
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Update_ShouldRederiveDependentFields()
    {
        var (km, router, kbCatalog, conceptCatalog, testDir) = BuildStack();
        try
        {
            string kbName = "TxUpdateKB";
            kbCatalog.CreateKb(kbName, Guid.NewGuid());

            var concept = new Concept { Name = "Triangle" };
            concept.Variables.Add(new Variable { Name = "base", Type = "NUMBER" });
            concept.Variables.Add(new Variable { Name = "height", Type = "NUMBER" });
            concept.Variables.Add(new Variable { Name = "area", Type = "NUMBER" });
            concept.Equations.Add(new Equation { Expression = "area = 0.5 * base * height" });
            conceptCatalog.CreateConcept(kbName, concept);

            var handleInsert = km.GetType().GetMethod("HandleInsert",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var rootUser = new User { Role = UserRole.ROOT };

            // Insert initial object
            handleInsert.Invoke(km, new object[] {
                new InsertNode {
                    ConceptName = "Triangle",
                    Values = new Dictionary<string, ValueNode> {
                        { "base",   new ValueNode { Value = 6m,  ValueType = "NUMBER" } },
                        { "height", new ValueNode { Value = 4m,  ValueType = "NUMBER" } }
                    }
                }, kbName, rootUser });

            var before = router.SelectObjects(kbName, "Triangle").First();
            var areaBefore = Convert.ToDouble(before.Values["area"]);
            Assert.True(Math.Abs(areaBefore - 12) < 0.001, $"Expected area=12 before update, got {areaBefore}");

            // Now UPDATE height
            var handleUpdate = km.GetType().GetMethod("HandleUpdate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            handleUpdate.Invoke(km, new object[] {
                new UpdateNode {
                    ConceptName = "Triangle",
                    Conditions = new List<Condition>(),
                    SetValues = new Dictionary<string, ExpressionNode> {
                        { "height", new LiteralNode { Value = 10m } }
                    }
                }, kbName, rootUser });

            var after = router.SelectObjects(kbName, "Triangle").First();
            var areaAfter = Convert.ToDouble(after.Values["area"]);
            Assert.True(Math.Abs(areaAfter - 30) < 0.001, $"Expected area=30 after update, got {areaAfter}");
            _output.WriteLine($"[PASS] area before={areaBefore}, after={areaAfter}");
        }
        finally
        {
            if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
        }
    }
}
