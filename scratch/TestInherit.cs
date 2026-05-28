using System;
using System.Collections.Generic;
using KBMS.Models;
using KBMS.Storage;
using KBMS.Storage.Core;
using KBMS.Knowledge;
using KBMS.Knowledge.Core;

class TestInheritance {
    static void Main() {
        var pool = new StoragePool("test_data", 256, "secret");
        var kbCatalog = new KbCatalog(pool);
        var conceptCatalog = new ConceptCatalog(pool);
        var userCatalog = new UserCatalog(pool);
        var km = new KnowledgeManager(pool, kbCatalog, conceptCatalog, userCatalog);
        
        var user = new User { Username = "root", Role = UserRole.ROOT };
        
        // 1. Create KB
        km.Execute(new KBMS.Parser.Ast.Kdl.CreateKbNode { KbName = "test" }, user, null);
        
        // 2. Create Parent
        var parentNode = new KBMS.Parser.Ast.Kdl.CreateConceptNode {
            ConceptName = "Parent",
            Variables = new List<KBMS.Models.Variable> {
                new KBMS.Models.Variable { Name = "pName", Type = "STRING" }
            }
        };
        km.Execute(parentNode, user, "test");
        
        // 3. Create Child
        var childNode = new KBMS.Parser.Ast.Kdl.CreateConceptNode {
            ConceptName = "Child",
            BaseObjects = new List<string> { "Parent" },
            Variables = new List<KBMS.Models.Variable> {
                new KBMS.Models.Variable { Name = "cAge", Type = "INT" }
            }
        };
        km.Execute(childNode, user, "test");
        
        // 4. Insert into Child (using parent variable)
        var insertNode = new KBMS.Parser.Ast.Kml.InsertNode {
            ConceptName = "Child",
            Values = new Dictionary<string, KBMS.Parser.Ast.Expressions.ExpressionNode> {
                ["pName"] = new KBMS.Parser.Ast.Expressions.LiteralNode { Value = "Father" },
                ["cAge"] = new KBMS.Parser.Ast.Expressions.LiteralNode { Value = 30 }
            }
        };
        var result = km.Execute(insertNode, user, "test");
        Console.WriteLine($"Insert Result: {result}");
        
        // 5. Select from Child
        var selectNode = new KBMS.Parser.Ast.Kql.SelectNode {
            ConceptName = "Child",
            SelectColumns = new List<KBMS.Parser.Ast.Kql.SelectColumn> {
                new KBMS.Parser.Ast.Kql.SelectColumn { IsStar = true }
            }
        };
        var selectResult = km.Execute(selectNode, user, "test");
        if (selectResult is QueryResultSet qrs) {
            Console.WriteLine($"Select Success: {qrs.Success}, Count: {qrs.Count}");
            foreach (var obj in qrs.Objects) {
                foreach (var kv in obj.Values) {
                    Console.WriteLine($"  {kv.Key}: {kv.Value}");
                }
            }
        } else {
            Console.WriteLine($"Select Failed: {selectResult}");
        }
        
        pool.Dispose();
    }
}
