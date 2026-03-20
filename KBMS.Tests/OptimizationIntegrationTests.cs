using System;
using System.Linq;
using KBMS.CLI;
using KBMS.Models;
using KBMS.Parser;
using KBMS.Storage;
using Xunit;
using Xunit.Abstractions;

namespace KBMS.Tests;

public class OptimizationIntegrationTests : IDisposable
{
    private readonly string _dataDir;
    private readonly StorageEngine _storage;
    private readonly KBMS.Knowledge.KnowledgeManager _km;
    private readonly User _root;

    public OptimizationIntegrationTests()
    {
        _dataDir = Path.Combine(Path.GetTempPath(), "KBMS_Opt_Test_" + Guid.NewGuid().ToString("N"));
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

    private dynamic Exec(string query, string kb = "opt_kb")
    {
        var lexer = new Lexer(query);
        var tokens = lexer.Tokenize();
        var parser = new Parser.Parser(tokens);
        var ast = parser.Parse();
        return _km.Execute(ast!, _root, kb);
    }

    [Fact]
    public void Test_OptimizationCommands()
    {
        // 1. Create KB and Schema
        Exec("CREATE KNOWLEDGE BASE opt_kb;", "system");
        Exec("CREATE CONCEPT Person ( VARIABLES ( age: int, name: string ) );");

        // 2. Insert Data
        Exec("INSERT INTO Person ATTRIBUTE ( name: 'Alice', age: 25 );");
        Exec("INSERT INTO Person ATTRIBUTE ( name: 'Bob', age: 30 );");

        // 3. Create Index
        var idxRes = Exec("CREATE INDEX idx_person_age ON Person ( age );");
        var resStr = System.Text.Json.JsonSerializer.Serialize((object)idxRes);
        Assert.Contains("success", resStr, StringComparison.OrdinalIgnoreCase);

        // 4. Maintenance Vacuum
        var vacRes = Exec("MAINTENANCE ( VACUUM );");
        resStr = System.Text.Json.JsonSerializer.Serialize((object)vacRes);
        Assert.Contains("VACUUM", resStr, StringComparison.OrdinalIgnoreCase);

        // 5. Maintenance Reindex & Check Consistency
        var maintRes = Exec("MAINTENANCE ( REINDEX ( Person ), CHECK ( CONSISTENCY: * ) );");
        resStr = System.Text.Json.JsonSerializer.Serialize((object)maintRes);
        Assert.Contains("REINDEX", resStr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CHECK_CONSISTENCY", resStr, StringComparison.OrdinalIgnoreCase);

        // 6. Explain
        var expRes = Exec("EXPLAIN ( SOLVE ON CONCEPT Person GIVEN age: 25 FIND name );");
        resStr = System.Text.Json.JsonSerializer.Serialize((object)expRes);
        Assert.Contains("plan", resStr, StringComparison.OrdinalIgnoreCase);
    }
}
