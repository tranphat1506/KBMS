using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using KBMS.Models;
using KBMS.Storage;
using KBMS.Knowledge;
using KBMS.Parser.Ast;

namespace KBMS.Tests.Phase1;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("--- PHASE 1 INTEGRATION TEST (WITH DEBUG) ---");
        
        string dataDir = "test_data";
        string encryptionKey = "test-key-12345678";
        
        if (Directory.Exists(dataDir)) Directory.Delete(dataDir, true);
        
        var storage = new StorageEngine(dataDir, encryptionKey);
        var manager = new KnowledgeManager(storage);
        var user = new User { Username = "root", Role = UserRole.ROOT, SystemAdmin = true };
        
        // 1. Create KB
        Console.WriteLine("\n[1] Creating KB: TestPhase1...");
        var kbRes = manager.Execute(new CreateKbNode { KbName = "TestPhase1" }, user, null);
        Console.WriteLine($"Result: {JsonSerializer.Serialize(kbRes)}");
        
        // 2. Create Concept
        Console.WriteLine("\n[2] Creating Concept <Item>...");
        var createConcept = new CreateConceptNode { 
            ConceptName = "Item",
            Variables = new List<VariableDefinition> { 
                new VariableDefinition { Name = "id", Type = "INT" } 
            }
        };
        var conceptRes = manager.Execute(createConcept, user, "TestPhase1");
        Console.WriteLine($"Result: {JsonSerializer.Serialize(conceptRes)}");

        // Verify .kmf file naming
        string kmfPath = Path.Combine(dataDir, "TestPhase1", "concepts.kmf");
        bool kmfExists = File.Exists(kmfPath);
        Console.WriteLine($"Checking .kmf exists at {Path.GetFullPath(kmfPath)}: {kmfExists} (Expect: True)");
        
        if (!kmfExists)
        {
            Console.WriteLine("FILES IN KB DIR:");
            if (Directory.Exists(Path.Combine(dataDir, "TestPhase1")))
            {
                foreach (var f in Directory.GetFiles(Path.Combine(dataDir, "TestPhase1")))
                    Console.WriteLine($"- {Path.GetFileName(f)}");
            }
            throw new Exception("KMF file not created!");
        }

        // 3. Test Transaction
        Console.WriteLine("\n[3] BEGIN TRANSACTION...");
        storage.BeginTransaction();
        
        Console.WriteLine("[4] INSERT (Shadow Pool only)...");
        var insert = new InsertNode { 
            ConceptName = "Item", 
            Values = new Dictionary<string, ValueNode> { 
                { "id", new ValueNode { ValueType = "NUMBER", Value = 101 } } 
            } 
        };
        manager.Execute(insert, user, "TestPhase1");
        
        string kdfPath = Path.Combine(dataDir, "TestPhase1", "objects.kdf");
        Console.WriteLine($"Checking .kdf exists: {File.Exists(kdfPath)} (Expect: False)");
        
        // 4. Test COMMIT
        Console.WriteLine("[5] COMMIT...");
        storage.CommitTransaction("TestPhase1");
        Console.WriteLine($"Checking .kdf exists: {File.Exists(kdfPath)} (Expect: True)");
        
        // 5. Test ROLLBACK
        Console.WriteLine("\n[6] BEGIN TRANSACTION (Second Case)...");
        storage.BeginTransaction();
        manager.Execute(new InsertNode { 
            ConceptName = "Item", 
            Values = new Dictionary<string, ValueNode> { 
                { "id", new ValueNode { ValueType = "NUMBER", Value = 999 } } 
            } 
        }, user, "TestPhase1");
        
        Console.WriteLine("[7] ROLLBACK...");
        storage.Rollback();
        
        var items = storage.SelectObjects("TestPhase1");
        Console.WriteLine($"Object Count: {items.Count} (Expect: 1)");
        
        Console.WriteLine("\n--- TEST FINISHED ---");
    }
}
