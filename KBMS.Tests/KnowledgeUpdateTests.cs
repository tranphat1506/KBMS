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
        var storage = new StorageEngine(testDir, "test-key");
        var km = new KnowledgeManager(storage);

        string kbName = "UpdateTestKB";
        storage.CreateKb(kbName, Guid.NewGuid());
        
        // Create concept
        var concept = new Concept { Name = "Product" };
        concept.Variables.Add(new Variable { Name = "id", Type = "INT" });
        concept.Variables.Add(new Variable { Name = "stock", Type = "INT" });
        storage.CreateConcept(kbName, concept);

        // Insert initial object
        var obj = new ObjectInstance {
            Id = Guid.NewGuid(),
            ConceptName = "Product",
            Values = new Dictionary<string, object> { { "id", 101L }, { "stock", 50L } }
        };
        storage.InsertObject(kbName, obj);

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
        var updatedObjects = storage.SelectObjects(kbName, new Dictionary<string, object> { { "id", 101L } });
        Assert.Single(updatedObjects);
        Assert.Equal(49, Convert.ToInt64(updatedObjects[0].Values["stock"]));

        // Cleanup
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }
}
