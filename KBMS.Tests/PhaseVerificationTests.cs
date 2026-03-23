using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KBMS.CLI;
using KBMS.Network;
using KBMS.Models;
using KBMS.Server;
using KBMS.Storage;
using KBMS.Knowledge;
using Xunit;

namespace KBMS.Tests;

/// <summary>
/// Dedicated integration tests for verifying functional phases 1, 2, 3, 4, and 6.
/// </summary>
public class PhaseVerificationTests : IAsyncLifetime
{
    private KbmsServer? _server;
    private Cli? _cli;
    private readonly int _testPort;
    private const string TestHost = "localhost";
    private readonly string _testDataDir;

    private static int _nextPort = 35000;
    private static int GetNextPort() => Interlocked.Increment(ref _nextPort);

    public PhaseVerificationTests()
    {
        _testPort = GetNextPort();
        _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_phase_verify_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        if (Directory.Exists(_testDataDir)) Directory.Delete(_testDataDir, true);

        // Start test server
        _server = new KbmsServer(TestHost, _testPort, _testDataDir);
        _ = _server.StartAsync();

        // Wait for server
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(50);
            try
            {
                _cli = new Cli(TestHost, _testPort);
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
    public async Task Phase1_DescribeHierarchy_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE p1_kb;");
        await _cli.ExecuteCommandAsync("USE p1_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Person ( VARIABLES (name: string) );");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Employee ( VARIABLES (id: int) );");
        await _cli.ExecuteCommandAsync("ADD HIERARCHY Employee IS_A Person;");

        // Test Child:Parent syntax
        var res1 = await _cli.ExecuteCommandAsync("DESCRIBE HIERARCHY Employee:Person;");
        Assert.Equal(MessageType.RESULT, res1!.Type);
        Assert.Contains("Employee", res1.Content);
        Assert.Contains("Person", res1.Content);

        // Test IS_A syntax
        var res2 = await _cli.ExecuteCommandAsync("DESCRIBE HIERARCHY Employee IS_A Person;");
        Assert.Equal(MessageType.RESULT, res2!.Type);
        Assert.Contains("Employee", res2.Content);
        Assert.Contains("Person", res2.Content);
    }

    [Fact]
    public async Task Phase2_SelectAliases_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE p2_kb;");
        await _cli.ExecuteCommandAsync("USE p2_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Product ( VARIABLES (name: string, price: double) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE (name:'Apple', price:1.5);");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE (name:'Banana', price:0.8);");

        // Test Column and Table aliases
        var query = "SELECT p.name AS ProductName, p.price * 1.1 AS PriceWithTax FROM Product p;";
        var res = await _cli.ExecuteCommandAsync(query);
        
        Assert.True(res!.Type == MessageType.RESULT, $"Expected RESULT but got {res.Type}. Error: {res.Content}");
        Assert.Contains("ProductName", res.Content);
        Assert.Contains("PriceWithTax", res.Content);
        // Debug: if this fails, we want to see the JSON
        if (!res.Content.Contains("Apple"))
            throw new Exception($"Phase 2 FAIL. Content: {res.Content}");

        Assert.Contains("Apple", res.Content);
        Assert.Contains("Banana", res.Content);
    }

    [Fact]
    public async Task Phase3_SelectFromRule_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE p3_kb;");
        await _cli.ExecuteCommandAsync("USE p3_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Student ( VARIABLES (name: string, grade: double) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Student ATTRIBUTE (name:'Alice', grade:95);");
        await _cli.ExecuteCommandAsync("INSERT INTO Student ATTRIBUTE (name:'Bob', grade:60);");
        
        await _cli.ExecuteCommandAsync("CREATE RULE TopStudents IF Student(grade > 90) THEN Student(honor = 'High');");

        // Test SELECT FROM RULE
        var res = await _cli.ExecuteCommandAsync("SELECT * FROM RULE TopStudents;");
        Assert.True(res!.Type == MessageType.RESULT, $"Expected RESULT but got {res.Type}. Error: {res.Content}");
        
        // Debug
        if (!res.Content.Contains("Alice"))
             throw new Exception($"Phase 3 FAIL. Content: {res.Content}");

        Assert.Contains("Alice", res.Content);
        Assert.DoesNotContain("Bob", res.Content);
    }

    [Fact]
    public async Task Phase4_SelectSubEntity_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE p4_kb;");
        await _cli.ExecuteCommandAsync("USE p4_kb;");
        await _cli.ExecuteCommandAsync(@"
            CREATE CONCEPT Item ( 
                VARIABLES (name: string, stock: int) 
                CONSTRAINTS (stock >= 0)
                RULES (RULE LowStock IF Item(stock < 5) THEN Item(order: true))
            );");

        // Test variables sub-entity
        var res1 = await _cli.ExecuteCommandAsync("SELECT * FROM Concept Item.variables;");
        Assert.Contains("name", res1!.Content);
        Assert.Contains("stock", res1.Content);

        // Test constraints sub-entity
        var res2 = await _cli.ExecuteCommandAsync("SELECT * FROM Concept Item.constraints;");
        // Note: > might be escaped as \u003e in JSON
        Assert.Contains("stock", res2!.Content);
        Assert.Contains("0", res2.Content);

        // Test rules sub-entity
        var res3 = await _cli.ExecuteCommandAsync("SELECT * FROM Concept Item.rules;");
        Assert.Contains("LowStock", res3!.Content);
    }

    [Fact]
    public async Task Phase6_InsertBulk_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE p6_kb;");
        await _cli.ExecuteCommandAsync("USE p6_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Log ( VARIABLES (msg: string, level: int) );");

        // Test INSERT BULK
        var bulkQuery = "INSERT BULK INTO Log ATTRIBUTE (msg:'Start', level:1), (msg:'Error', level:3), (msg:'End', level:1);";
        var res = await _cli.ExecuteCommandAsync(bulkQuery);
        
        Assert.Equal(MessageType.RESULT, res!.Type);
        Assert.Contains("3 inserted", res.Content);
        Assert.Contains("0 failed", res.Content);

        // Verify data
        var selectRes = await _cli.ExecuteCommandAsync("SELECT * FROM Log;");
        Assert.Contains("Start", selectRes!.Content);
        Assert.Contains("Error", selectRes.Content);
        Assert.Contains("End", selectRes.Content);
    }
}
