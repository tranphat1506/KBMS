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
using System.Linq;

namespace KBMS.Tests;

public class WriteTimeInferenceTests
{
    [Fact]
    public void HandleInsert_ShouldCalculateEquationsAndSave()
    {
        // Setup temporary storage
        string testDir = Path.Combine(Path.GetTempPath(), "KBMS_Test_" + Guid.NewGuid().ToString());
        if (!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);
        var pool = new KBMS.Storage.Core.StoragePool(testDir, 64);
        var kbCatalog = new KBMS.Storage.Core.KbCatalog(pool);
        var conceptCatalog = new KBMS.Storage.Core.ConceptCatalog(pool);
        var userCatalog = new KBMS.Storage.Core.UserCatalog(pool);

        var router = new KBMS.Knowledge.Core.StorageRouter(pool);
        var km = new KnowledgeManager(pool, kbCatalog, conceptCatalog, userCatalog, router);

        string kbName = "InferenceTestKB";
        kbCatalog.CreateKb(kbName, Guid.NewGuid());
        
        var concept = new Concept { Name = "Rectangle" };
        concept.Variables.Add(new Variable { Name = "width", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "height", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "area", Type = "INT" });
        // Add Equation: area = width * height
        concept.Equations.Add(new Equation { Expression = "area = width * height", Variables = new List<string>{"area", "width", "height"} });
        
        conceptCatalog.CreateConcept(kbName, concept);

        // Create insert node: INSERT INTO Rectangle (width: 5, height: 10)
        var insertNode = new InsertNode {
            ConceptName = "Rectangle",
            Values = new Dictionary<string, ValueNode> {
                { "width", new ValueNode { Value = 5L, ValueType = "NUMBER" } },
                { "height", new ValueNode { Value = 10L, ValueType = "NUMBER" } }
            }
        };

        // Execute insert
        var result = km.GetType().GetMethod("HandleInsert", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(km, new object[] { insertNode, kbName, new User { Role = UserRole.ROOT } });

        // Verify value in storage
        var insertedObjects = km.V3Router.SelectObjects(kbName, "Rectangle");
        Assert.Single(insertedObjects);
        var targetObj = insertedObjects.First();
        
        // Assert Write-Time Inference successfully deduced the area and saved it
        Assert.Equal(50.0, Convert.ToDouble(targetObj.Values["area"]));

        // Cleanup
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }

    [Fact]
    public void HandleUpdate_ShouldRecalculateEquationsAndSave()
    {
        // Setup temporary storage
        string testDir = Path.Combine(Path.GetTempPath(), "KBMS_Test_" + Guid.NewGuid().ToString());
        if (!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);
        var pool = new KBMS.Storage.Core.StoragePool(testDir, 64);
        var kbCatalog = new KBMS.Storage.Core.KbCatalog(pool);
        var conceptCatalog = new KBMS.Storage.Core.ConceptCatalog(pool);
        var userCatalog = new KBMS.Storage.Core.UserCatalog(pool);

        var router = new KBMS.Knowledge.Core.StorageRouter(pool);
        var km = new KnowledgeManager(pool, kbCatalog, conceptCatalog, userCatalog, router);

        string kbName = "InferenceUpdateTestKB";
        kbCatalog.CreateKb(kbName, Guid.NewGuid());
        
        var concept = new Concept { Name = "Rectangle" };
        concept.Variables.Add(new Variable { Name = "id", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "width", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "height", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "area", Type = "INT" });
        // Add Equation: area = width * height
        concept.Equations.Add(new Equation { Expression = "area = width * height", Variables = new List<string>{"area", "width", "height"} });
        
        conceptCatalog.CreateConcept(kbName, concept);
        
        // Insert base object manually
        var obj = new ObjectInstance {
            Id = Guid.NewGuid(),
            ConceptName = "Rectangle",
            Values = new Dictionary<string, object> { { "id", 1L }, { "width", 5L }, { "height", 10L }, { "area", 50L } }
        };
        router.InsertObject(kbName, obj);

        // Create update node: UPDATE Rectangle VARIABLES (SET height: 20) WHERE id = 1
        var updateNode = new UpdateNode {
            ConceptName = "Rectangle",
            SetValues = new Dictionary<string, ExpressionNode> {
                { "height", new LiteralNode { Value = 20L } }
            },
            Conditions = new List<Condition> {
                new Condition { Field = "id", Operator = "=", Value = 1L }
            }
        };

        // Execute update
        var result = km.GetType().GetMethod("HandleUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(km, new object[] { updateNode, kbName, new User { Role = UserRole.ROOT } });

        // Verify value in storage
        var updatedObjects = km.V3Router.SelectObjects(kbName, "Rectangle");
        Assert.Single(updatedObjects);
        var targetObj = updatedObjects.First();
        
        // Assert Write-Time Inference successfully RE-deduced the area and saved it
        Assert.Equal(100.0, Convert.ToDouble(targetObj.Values["area"]));
        Assert.Equal(20L, Convert.ToInt64(targetObj.Values["height"]));

        // Cleanup
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }
}
