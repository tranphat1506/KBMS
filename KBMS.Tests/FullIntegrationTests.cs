using KBMS.Models;
using KBMS.CLI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KBMS.Network;
using Xunit;

namespace KBMS.Tests;

public class FullIntegrationTests : IAsyncLifetime
{
    private Server.KbmsServer? _server;
    private Cli? _cli;
    private readonly int _testPort;
    private readonly string _testDataDir;

    private static int _nextPort = 37000;
    private static int GetNextPort() => Interlocked.Increment(ref _nextPort);

    public FullIntegrationTests()
    {
        _testPort = GetNextPort();
        _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_full_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        if (Directory.Exists(_testDataDir)) Directory.Delete(_testDataDir, true);
        var storage = new Storage.StorageEngine(_testDataDir, "full_test_key");
        _server = new Server.KbmsServer("localhost", _testPort, _testDataDir);
        _ = _server.StartAsync();

        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(50);
            try
            {
                _cli = new Cli("localhost", _testPort);
                await _cli.ConnectAsync(autoReconnect: false);
                await _cli.ExecuteCommandAsync("LOGIN root root");
                return;
            }
            catch { _cli = null; }
        }
        throw new Exception("Failed to connect to test server");
    }

    public async Task DisposeAsync()
    {
        if (_cli != null) await _cli.DisconnectAsync();
        _server?.Stop();
        try { if (Directory.Exists(_testDataDir)) Directory.Delete(_testDataDir, true); } catch { }
    }

    [Fact]
    public async Task Section1_Setup_And_ForwardChaining_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE EnterpriseKB;");
        await _cli.ExecuteCommandAsync("USE EnterpriseKB;");
        
        await _cli.ExecuteCommandAsync(@"
            CREATE CONCEPT Student (
                VARIABLES (
                    id: INT,
                    name: STRING,
                    grade: DECIMAL,
                    honor: STRING,
                    gifted: BOOLEAN
                )
            );");

        await _cli.ExecuteCommandAsync("CREATE RULE HighHonor SCOPE Student IF grade >= 90 THEN SET honor = 'High';");
        await _cli.ExecuteCommandAsync("CREATE RULE HonorToGift SCOPE Student IF honor = 'High' THEN SET gifted = true;");

        // Use SOLVE to infer and save
        await _cli.ExecuteCommandAsync("SOLVE ON CONCEPT Student GIVEN id: 1, name: 'Alice', grade: 95 FIND honor, gifted SAVE;");
        
        var res = await _cli.ExecuteCommandAsync("SELECT name, honor, gifted FROM Student WHERE name = 'Alice';");
        var resLower = res!.Content.ToLower();
        Assert.Contains("high", resLower);
        Assert.Contains("true", resLower);

        // Verification of WHERE clause on derived string (Testing quote stripping fix)
        var resWhere = await _cli.ExecuteCommandAsync("SELECT * FROM Student WHERE honor = 'High';");
        Assert.Equal(MessageType.RESULT, resWhere!.Type);
        Assert.Contains("Alice", resWhere.Content); // Should NOT be empty
    }

    [Fact]
    public async Task Section2_BackwardChaining_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE BackKB;");
        await _cli.ExecuteCommandAsync("USE BackKB;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Student ( VARIABLES (grade: DECIMAL, honor: STRING, gifted: BOOLEAN) );");
        await _cli.ExecuteCommandAsync("CREATE RULE R1 SCOPE Student IF grade >= 90 THEN SET honor = 'High';");
        await _cli.ExecuteCommandAsync("CREATE RULE R2 SCOPE Student IF honor = 'High' THEN SET gifted = true;");

        // SOLVE FIND triggers Backward Chaining (without inserting Alice yet)
        var res = await _cli.ExecuteCommandAsync("SOLVE ON CONCEPT Student GIVEN grade: 92 FIND gifted;");
        
        Assert.Equal(MessageType.RESULT, res!.Type);
        Assert.Contains("Derived Fact: gifted = True", res.Content);
        Assert.Contains("Rule R1 resolved honor", res.Content);
        Assert.Contains("Rule R2 resolved gifted", res.Content);
    }

    [Fact]
    public async Task Section4_Metadata_And_Aliases()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE MetaKB;");
        await _cli.ExecuteCommandAsync("USE MetaKB;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Product (VARIABLES(id: INT, price: DECIMAL, stock: INT));");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE (501, 1000.0, 50);");

        var res = await _cli.ExecuteCommandAsync(@"
            SELECT 
                p.id AS ProductID, 
                p.price * 1.1 AS PriceWithVAT
            FROM Product p;");
        
        Assert.Contains("ProductID", res!.Content);
        Assert.Contains("PriceWithVAT", res.Content);
        Assert.Contains("1100", res.Content);
    }

    [Fact]
    public async Task Section5_TableAliasedWhereClause_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE AliasKB;");
        await _cli.ExecuteCommandAsync("USE AliasKB;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Product (VARIABLES(id: INT, price: DECIMAL));");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE (1, 1000);");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE (2, 200);");

        // WHERE clause with alias prefix 'p.'
        var res = await _cli.ExecuteCommandAsync("SELECT p.id FROM Product p WHERE p.price > 500;");
        // Check for specific value in the result list part of the JSON to avoid metadata collisions
        Assert.Contains("[{\"id\":1}]", res!.Content);
        Assert.DoesNotContain("[{\"id\":2}]", res.Content);
    }

    [Fact]
    public async Task Section6_JoinWithAliases_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE JoinKB;");
        await _cli.ExecuteCommandAsync("USE JoinKB;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Dept (VARIABLES(id: INT, name: STRING));");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Emp (VARIABLES(id: INT, name: STRING, dept_id: INT));");
        
        await _cli.ExecuteCommandAsync("INSERT INTO Dept ATTRIBUTE (1, 'IT');");
        await _cli.ExecuteCommandAsync("INSERT INTO Emp ATTRIBUTE (101, 'Alice', 1);");

        // JOIN with aliases and qualified ON condition
        var res = await _cli.ExecuteCommandAsync(@"
            SELECT e.name, d.name AS DeptName 
            FROM Emp e 
            JOIN Dept d ON e.dept_id = d.id;");
        
        Assert.Equal(MessageType.RESULT, res!.Type);
        Assert.Contains("Alice", res.Content);
        Assert.Contains("IT", res.Content);
        Assert.Contains("DeptName", res.Content);
    }

    [Fact]
    public async Task Section7_ExpressionsAndTrailingCommas_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE ExprKB;");
        await _cli.ExecuteCommandAsync("USE ExprKB;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Product (VARIABLES(id: INT, price: DECIMAL));");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE (1, 1000);");

        // 1. Expression with alias (should NOT be null)
        var resExpr = await _cli.ExecuteCommandAsync("SELECT p.price * 1.1 AS Result FROM Product p;");
        Assert.Equal(MessageType.RESULT, resExpr!.Type);
        Assert.Contains("1100", resExpr.Content);
        Assert.DoesNotContain("null", resExpr.Content.ToLower());

        // 2. Trailing comma should FAIL with syntax error
        var resComma = await _cli.ExecuteCommandAsync("SELECT id, price, FROM Product;");
        Assert.Equal(MessageType.ERROR, resComma!.Type);
        Assert.Contains("Trailing comma", resComma.Content);
    }
}
