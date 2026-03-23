using Xunit;
using System.Collections.Generic;
using KBMS.Models;
using KBMS.Knowledge;
using KBMS.Storage;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Expressions;
using KBMS.Parser.Ast;
using System.IO;
using System;

namespace KBMS.Tests;

public class KnowledgeUpdateTests
{
    [Fact]
    public void HandleUpdate_ShouldEvaluateExpressionsPerObject()
    {
        // Setup temporary storage
        string testDir = Path.Combine(Path.GetTempPath(), "KBMS_Test_" + Guid.NewGuid().ToString());
        if (!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);
        string dbFile = Path.Combine(testDir, "test.kdb");

        var diskManager = new KBMS.Storage.V3.DiskManager(dbFile);
        var bpm = new KBMS.Storage.V3.BufferPoolManager(diskManager, 64);
        var wal = new KBMS.Storage.V3.WalManagerV3(dbFile);
        var kbCatalog = new KBMS.Storage.V3.KbCatalog(bpm, diskManager);
        var conceptCatalog = new KBMS.Storage.V3.ConceptCatalog(bpm, diskManager);
        var userCatalog = new KBMS.Storage.V3.UserCatalog(bpm, diskManager);

        var router = new KBMS.Knowledge.V3.V3DataRouter(bpm, diskManager);
        var km = new KnowledgeManager(bpm, diskManager, kbCatalog, conceptCatalog, userCatalog, wal, router);

        string kbName = "UpdateTestKB";
        kbCatalog.CreateKb(kbName, Guid.NewGuid());
        
        var concept = new Concept { Name = "Product" };
        concept.Variables.Add(new Variable { Name = "id", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "stock", Type = "INT" });
        conceptCatalog.CreateConcept(kbName, concept);
        var obj = new ObjectInstance {
            Id = Guid.NewGuid(),
            ConceptName = "Product",
            Values = new Dictionary<string, object> { { "id", 101L }, { "stock", 50L } }
        };
        router.InsertObject(kbName, obj);

        // Create update node: UPDATE Product ATTRIBUTE (SET stock: stock - 1) WHERE id = 101
        var updateNode = new UpdateNode {
            ConceptName = "Product",
            SetValues = new Dictionary<string, ExpressionNode> {
                { "stock", new BinaryExpressionNode {
                    Left = new VariableNode { Name = "stock" },
                    Operator = "-",
                    Right = new LiteralNode { Value = 1L }
                } }
            },
            Conditions = new List<Condition> {
                new Condition { Field = "id", Operator = "=", Value = 101L }
            }
        };

        // Execute update
        var result = km.GetType().GetMethod("HandleUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(km, new object[] { updateNode, kbName });

        // Assert success using reflection
        var successProp = result.GetType().GetProperty("success");
        var isSuccess = (bool)successProp.GetValue(result);
        Assert.True(isSuccess);

        // Verify value in storage
        var updatedObjects = km.V3Router.SelectObjects(kbName, "Product");
        var targetObj = updatedObjects.FirstOrDefault(o => Convert.ToInt64(o.Values["id"]) == 101L);
        Assert.NotNull(targetObj);
        Assert.Equal(49, Convert.ToInt64(targetObj.Values["stock"]));

        // Cleanup
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }
}
