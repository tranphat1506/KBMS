using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Parser;
using KBMS.Storage;
using Xunit;

namespace KBMS.Tests;

public class Phase5IntegrationTests : IDisposable
{
    private readonly string _dataDir;
    private readonly StorageEngine _storage;
    private readonly KBMS.Knowledge.KnowledgeManager _km;
    private readonly User _root;

    public Phase5IntegrationTests()
    {
        _dataDir = Path.Combine(Path.GetTempPath(), "KBMS_P5_Test_" + Guid.NewGuid().ToString("N"));
        _storage = new StorageEngine(_dataDir, "testkey");
        _km = new KBMS.Knowledge.KnowledgeManager(_storage);
        _root = new User { Username = "root", SystemAdmin = true };
        _storage.SaveUsers(new List<User> { _root });
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDir))
            Directory.Delete(_dataDir, true);
    }

    private dynamic Exec(string query, string kb = "p5_kb")
    {
        var lexer = new Lexer(query);
        var tokens = lexer.Tokenize();
        var parser = new Parser.Parser(tokens);
        var ast = parser.Parse();
        return _km.Execute(ast!, _root, kb);
    }

    private string Json(object o) => System.Text.Json.JsonSerializer.Serialize(o);

    [Fact]
    public void Test_Describe_Concept()
    {
        // Setup
        Exec("CREATE KNOWLEDGE BASE p5_kb;", "system");
        Exec("CREATE CONCEPT Animal ( VARIABLES ( name: string, legs: int ) );");

        // DESCRIBE CONCEPT
        var res = Exec("DESCRIBE ( CONCEPT: Animal );");
        var s = Json((object)res);
        Assert.Contains("\"Success\":true", s);
        Assert.Contains("name", s);
        Assert.Contains("legs", s);
    }

    [Fact]
    public void Test_Describe_KB()
    {
        Exec("CREATE KNOWLEDGE BASE p5_kb;", "system");
        Exec("CREATE CONCEPT Cat ( VARIABLES ( name: string ) );");

        var res = Exec("DESCRIBE ( KB: p5_kb );");
        var s = Json((object)res);
        Assert.Contains("\"Success\":true", s);
        Assert.Contains("p5_kb", s);
    }

    [Fact]
    public void Test_Export_And_Import()
    {
        Exec("CREATE KNOWLEDGE BASE p5_kb;", "system");
        Exec("CREATE CONCEPT Car ( VARIABLES ( brand: string, speed: int ) );");
        Exec("INSERT INTO Car ATTRIBUTE ( brand: 'Tesla', speed: 200 );");
        Exec("INSERT INTO Car ATTRIBUTE ( brand: 'BMW', speed: 250 );");

        // Use _dataDir for export so path is guaranteed to exist
        var exportPath = Path.Combine(_dataDir, "car_export.json");

        // EXPORT
        var expRes = Exec($"EXPORT ( CONCEPT: Car, FORMAT: JSON, FILE: '{exportPath}' );");
        var expStr = Json((object)expRes);
        Assert.Contains("\"success\":true", expStr);
        Assert.Contains("\"exported\":2", expStr);
        Assert.True(File.Exists(exportPath), $"Expected file: {exportPath}");

        // DELETE all and IMPORT back
        Exec("DELETE FROM Car;");

        var impRes = Exec($"IMPORT ( CONCEPT: Car, FORMAT: JSON, FILE: '{exportPath}' );");
        var impStr = Json((object)impRes);
        Assert.Contains("\"success\":true", impStr);
        Assert.Contains("\"imported\":2", impStr);
    }

    [Fact]
    public void Test_CreateTrigger_Parses()
    {
        Exec("CREATE KNOWLEDGE BASE p5_kb;", "system");
        Exec("CREATE CONCEPT Product ( VARIABLES ( name: string ) );");

        // Use SOLVE in DO block to avoid SELECT * wildcard parse ambiguity in nested context
        var tRes = Exec("CREATE TRIGGER trg_product_insert ( ON ( INSERT OF Product ), DO ( SOLVE ON CONCEPT Product GIVEN name: 'X' FIND name ) );");
        var s = Json((object)tRes);
        Assert.Contains("\"success\":true", s);
        Assert.Contains("trg_product_insert", s);
    }
}
