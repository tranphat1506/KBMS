using System;
using System.Linq;
using System.Text.Json;
using KBMS.Server.Core;
using KBMS.Storage.Core;
using KBMS.Storage;
using Xunit;

namespace KBMS.Tests;

public class LspEngineTests
{
    [Fact]
    public void GetCompletions_ShouldSuggestKeywords()
    {
        // Mock a real ConceptCatalog by initializing a StoragePool
        // We will just test the basic keyword functionality which doesn't need data.
        var engine = new LspEngine(null!); 

        // Typing "CREA"
        var result = engine.GetCompletions("CREA", 1, 5, null);
        var json = JsonSerializer.Serialize(result);
        
        Assert.Contains("CREATE", json);
        Assert.DoesNotContain("CONCEPT", json);
    }

    [Fact]
    public void GetCompletions_ShouldSuggestDataTypes()
    {
        var engine = new LspEngine(null!); 

        // Typing "price: "
        var result = engine.GetCompletions("CREATE CONCEPT Item ( VARIABLES ( price: ", 1, 42, null);
        var json = JsonSerializer.Serialize(result);
        
        Assert.Contains("DECIMAL", json);
        Assert.Contains("STRING", json);
    }

    [Fact]
    public void GetDiagnostics_ShouldReturnSyntaxError()
    {
        var engine = new LspEngine(null!); 

        // Missing concept name will definitely throw
        var result = engine.GetDiagnostics("CREATE CONCEPT ;");
        var json = JsonSerializer.Serialize(result);
        
        Assert.Contains("\"valid\":false", json);
        Assert.Contains("Expected concept name", json);
        Assert.Contains("\"line\":1", json);
    }
    
    [Fact]
    public void GetDiagnostics_ShouldReturnValid()
    {
        var engine = new LspEngine(null!); 
        
        var result = engine.GetDiagnostics("CREATE CONCEPT S ( VARIABLES ( p: DECIMAL ) );");
        var json = JsonSerializer.Serialize(result);
        
        Assert.Contains("\"valid\":true", json);
        Assert.Contains("\"errors\":[]", json);
    }

    [Fact]
    public void GetCompletions_ShouldSuggestConceptVariables_InRuleIf()
    {
        // 1. Setup a real Catalog
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kbms_lsp_test_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);
        var storagePool = new StoragePool(tempDir);
        var catalog = new ConceptCatalog(storagePool);
        
        // 2. Create Knowledge Base and Concept
        // Setup initial schema
        var kbName = "lsp_kb";
        storagePool.GetManagers(kbName); // initialize db files
        
        var concept = new KBMS.Models.Concept { Name = "Person" };
        concept.Variables.Add(new KBMS.Models.Variable { Name = "age", Type = "DECIMAL" });
        catalog.CreateConcept(kbName, concept);
        
        var engine = new LspEngine(catalog);

        // 3. Test the context parsing where AST is partially broken due to typing in progress
        string code = "CREATE RULE R1 SCOPE Person p; IF p.";
        var result = engine.GetCompletions(code, 1, 37, "lsp_kb");
        var json = JsonSerializer.Serialize(result);

        Assert.Contains("age", json); // It should deeply lookup 'Person' and find 'age'
        Assert.Contains("Variable", json); // Kind should be mapped correctly
        
        System.IO.Directory.Delete(tempDir, true);
    }

    [Fact]
    public void GetCompletions_TolerantMode_ShouldHandleBrokenSyntax()
    {
        var engine = new LspEngine(null!);
        // User forgot closing parenthesis and semicolon, AST is broken
        string code = "CREATE CONCEPT Broken ( VARIABLES ( price: ";
        var result = engine.GetCompletions(code, 1, 44, null);
        var json = JsonSerializer.Serialize(result);

        // The parser should have tolerated the failure, recovered the PartialNode, 
        // and the hybrid lexer should enforce ExpectDataType context based on the colon.
        Assert.Contains("DECIMAL", json);
        Assert.Contains("STRING", json);
    }

    [Fact]
    public void GetCompletions_Performance_ShouldBeFast()
    {
        var engine = new LspEngine(null!);
        var codeBuilder = new System.Text.StringBuilder();
        for(int i = 0; i < 100; i++)
        {
            codeBuilder.AppendLine($"CREATE CONCEPT C{i} ( VARIABLES ( v{i}: DECIMAL ) );");
        }
        codeBuilder.Append("CREATE RULE R1 SCOPE C99 c; IF c.");
        string code = codeBuilder.ToString();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var result = engine.GetCompletions(code, 101, 34, null); // line 101, after 'c.'
        watch.Stop();

        // 100 lines of script parsed from scratch should take less than 100ms
        Assert.True(watch.ElapsedMilliseconds < 100, $"LSP performance too slow: {watch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void GetCompletions_MultiAlias_InRule()
    {
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kbms_lsp_test_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);
        var storagePool = new StoragePool(tempDir);
        var catalog = new ConceptCatalog(storagePool);
        
        var kbName = "lsp_kb";
        storagePool.GetManagers(kbName); 
        
        var patient = new KBMS.Models.Concept { Name = "Patient" };
        patient.Variables.Add(new KBMS.Models.Variable { Name = "age", Type = "DECIMAL" });
        catalog.CreateConcept(kbName, patient);

        var record = new KBMS.Models.Concept { Name = "Record" };
        record.Variables.Add(new KBMS.Models.Variable { Name = "diagnosis", Type = "STRING" });
        catalog.CreateConcept(kbName, record);
        
        var engine = new LspEngine(catalog);

        // Test multi-alias lookup: typing 'r.' should yield Record variables (diagnosis), not age
        string code = "CREATE RULE R1 SCOPE Patient p, Record r; IF r.";
        var result = engine.GetCompletions(code, 1, 48, kbName);
        var json = JsonSerializer.Serialize(result);

        Console.WriteLine("DEBUG LSP JSON: " + json);
        Assert.Contains("diagnosis", json);
        Assert.DoesNotContain("age", json); 
        
        System.IO.Directory.Delete(tempDir, true);
    }

    [Fact]
    public void GetCompletions_Select_Lookahead()
    {
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kbms_lsp_test_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);
        var storagePool = new StoragePool(tempDir);
        var catalog = new ConceptCatalog(storagePool);
        
        var kbName = "lsp_kb";
        storagePool.GetManagers(kbName); 
        
        var doctor = new KBMS.Models.Concept { Name = "Doctor" };
        doctor.Variables.Add(new KBMS.Models.Variable { Name = "specialty", Type = "STRING" });
        catalog.CreateConcept(kbName, doctor);
        
        var engine = new LspEngine(catalog);

        // Test look-ahead: parser crashes at 'SELECT d.', so it never reaches 'FROM Doctor d'.
        // The hybrid lexer should scan ahead, find 'Doctor d', populate SymbolTable, and resolve 'd.'
        string code = "SELECT d. FROM Doctor d;";
        var result = engine.GetCompletions(code, 1, 10, kbName); // cursor after d.
        var json = JsonSerializer.Serialize(result);

        Console.WriteLine("DEBUG LSP JSON: " + json);
        Assert.Contains("specialty", json);
        
        System.IO.Directory.Delete(tempDir, true);
    }
}
