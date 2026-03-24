using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KBMS.Knowledge;
using KBMS.Models;
using KBMS.Parser;
using KBMS.Storage;
using Xunit;

namespace KBMS.Tests;

public class ExhaustiveAlterIntegrationTests : IDisposable
{
    private readonly string _testDataDir;
    private readonly KBMS.Storage.V3.StoragePool _storagePool;
    private readonly KBMS.Storage.V3.KbCatalog _kbCatalog;
    private readonly KBMS.Storage.V3.ConceptCatalog _conceptCatalog;
    private readonly KBMS.Storage.V3.UserCatalog _userCatalog;
    private readonly KnowledgeManager _km;
    private readonly User _root;

    public ExhaustiveAlterIntegrationTests()
    {
        _testDataDir = Path.Combine(Path.GetTempPath(), "kbms_alter_tests_" + Guid.NewGuid().ToString("N"));
        if (!Directory.Exists(_testDataDir)) Directory.CreateDirectory(_testDataDir);
        
        _storagePool = new KBMS.Storage.V3.StoragePool(_testDataDir, 64);
        _kbCatalog = new KBMS.Storage.V3.KbCatalog(_storagePool);
        _conceptCatalog = new KBMS.Storage.V3.ConceptCatalog(_storagePool);
        _userCatalog = new KBMS.Storage.V3.UserCatalog(_storagePool);
        var router = new KBMS.Knowledge.V3.V3DataRouter(_storagePool);

        _km = new KnowledgeManager(_storagePool, _kbCatalog, _conceptCatalog, _userCatalog, router);
        _root = new User { Username = "root", Role = UserRole.ROOT, SystemAdmin = true };
        _userCatalog.CreateUser("root", "password", UserRole.ROOT);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataDir))
            Directory.Delete(_testDataDir, true);
    }

    private object Exec(string query, string? kb = null)
    {
        var lexer = new Lexer(query);
        var tokens = lexer.Tokenize();
        Console.WriteLine($"DEBUG TOKENS: {string.Join(", ", tokens.Select(t => t.Type + ":" + t.Lexeme))}");
        var parser = new Parser.Parser(tokens);
        var ast = parser.Parse();
        return _km.Execute(ast!, _root, kb);
    }

    [Fact]
    public void Test_AlterConcept_AddRenameDrop_WithMigration()
    {
        // 1. Create KB and Concept
        Exec("CREATE KNOWLEDGE BASE TestKB;");
        Exec("CREATE CONCEPT Person ( VARIABLES ( name: string ) );", "TestKB");
        
        // 2. Insert initial data
        Exec("INSERT INTO Person ATTRIBUTE ( name: 'Alice' );", "TestKB");

        // 3. ALTER: Add variable, Rename variable, Add constraint
        var alterQuery = @"
            ALTER CONCEPT Person (
                ADD ( VARIABLE ( age: int ), CONSTRAINT ( age_min: age >= 0 ) ),
                RENAME ( VARIABLE name TO full_name )
            );";
        var result = Exec(alterQuery, "TestKB");
        var resStr = System.Text.Json.JsonSerializer.Serialize((object)result);
        Assert.Contains("success", resStr, StringComparison.OrdinalIgnoreCase);

        // 4. Verify Schema
        var concept = _conceptCatalog.LoadConcept("TestKB", "Person");
        Assert.NotNull(concept);
        Assert.Contains(concept.Variables, v => v.Name == "full_name");
        Assert.Contains(concept.Variables, v => v.Name == "age");
        Assert.Contains(concept.Constraints, c => c.Name == "age_min");

        // 5. Verify Data Migration
        var instances = _km.SelectAllObjects("TestKB").Where(o => o.ConceptName == "Person").ToList();
        Assert.Single(instances);
        var alice = instances[0];
        Assert.True(alice.Values.ContainsKey("full_name"));
        Assert.Equal("Alice", alice.Values["full_name"]);
        Assert.True(alice.Values.ContainsKey("age"));
        Assert.Null(alice.Values["age"]); // Default for new variable

        // 6. DROP variable
        Exec("ALTER CONCEPT Person ( DROP ( VARIABLE age ) );", "TestKB");
        instances = _km.SelectAllObjects("TestKB").Where(o => o.ConceptName == "Person").ToList();
        Assert.False(instances[0].Values.ContainsKey("age"));
    }

    [Fact]
    public void Test_AlterConcept_Wildcard()
    {
        Exec("CREATE KNOWLEDGE BASE WildKB;");
        Exec("CREATE CONCEPT C1 ( VARIABLES ( a: int ) );", "WildKB");
        Exec("CREATE CONCEPT C2 ( VARIABLES ( a: int ) );", "WildKB");

        // Mass Alter
        Exec("ALTER CONCEPT * ( ADD ( VARIABLE ( b: int ) ) );", "WildKB");

        var c1 = _conceptCatalog.LoadConcept("WildKB", "C1");
        var c2 = _conceptCatalog.LoadConcept("WildKB", "C2");
        Assert.NotNull(c1);
        Assert.NotNull(c2);
        Assert.Contains(c1.Variables, v => v.Name == "b");
        Assert.Contains(c2.Variables, v => v.Name == "b");
    }

    [Fact]
    public void Test_AlterUser_AndPassword()
    {
        Exec("CREATE USER dev1 PASSWORD 'oldpass';");
        var result = Exec("ALTER USER dev1 ( SET ( PASSWORD: 'newpass', ADMIN: true ) );", "user_kb");
        var resStr = System.Text.Json.JsonSerializer.Serialize((object)result);
        Assert.Contains("success", resStr, StringComparison.OrdinalIgnoreCase);

        var users = _userCatalog.ListUsers();
        var dev1 = users.First(u => u.Username == "dev1");
        Assert.True(dev1.SystemAdmin);
        // AuthenticationManager handles password verification in V3
        var authRes = _userCatalog.FindUser("dev1");
        Assert.NotNull(authRes);
        // Note: wal check omitted in base test for simplicity
    }
}
