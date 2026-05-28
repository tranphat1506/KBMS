using KBMS.Parser.Ast.Kql;
using KBMS.Models;
using KBMS.CLI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KBMS.Tests;

public class SemanticQueryTests : IAsyncLifetime
{
    private Server.KbmsServer? _server;
    private Cli? _cli;
    private readonly int _testPort;
    private readonly string _testDataDir;

    private static int _nextPort = 37000;
    private static int GetNextPort() => Interlocked.Increment(ref _nextPort);

    public SemanticQueryTests()
    {
        _testPort = GetNextPort();
        _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_semantic_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        if (Directory.Exists(_testDataDir)) Directory.Delete(_testDataDir, true);
        var storage = new Storage.StorageEngine(_testDataDir, "semantic_key");
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
    public async Task Find_WithIsStuck_ShouldFilterCorrectly()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE sem_kb;");
        await _cli.ExecuteCommandAsync("USE sem_kb;");
        
        await _cli.ExecuteCommandAsync(@"
            CREATE CONCEPT Patient (
                VARIABLES (
                    symptom: STRING,
                    disease: STRING
                )
            );");

        await _cli.ExecuteCommandAsync("CREATE RULE R1 SCOPE Patient IF symptom = 'fever' THEN SET disease = 'flu';");
        
        // P1 will NOT fire (missing symptom) -> should be STUCK
        await _cli.ExecuteCommandAsync("INSERT INTO Patient VARIABLES();"); 
        
        // P2 WILL fire (has symptom) -> should NOT be STUCK
        await _cli.ExecuteCommandAsync("INSERT INTO Patient VARIABLES(symptom: 'fever');");

        // Use FIND with IS_STUCK()
        var resStuck = await _cli.ExecuteCommandAsync("FIND Patient WITH IS_STUCK() RETURN MISSING_FACTS();");
        Assert.Contains("symptom", resStuck!.Content.ToLower()); // It tells us symptom is missing
        Assert.DoesNotContain("fever", resStuck.Content.ToLower()); // The second patient is NOT stuck

        var resAll = await _cli.ExecuteCommandAsync("FIND Patient RETURN disease;");
        Assert.Contains("flu", resAll!.Content.ToLower());
    }

    [Fact]
    public async Task Find_WithHasFired_And_Explainability_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE sem2_kb;");
        await _cli.ExecuteCommandAsync("USE sem2_kb;");
        
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Item ( VARIABLES (price: DECIMAL, tax: DECIMAL) );");
        await _cli.ExecuteCommandAsync("CREATE RULE CalcTax SCOPE Item IF price > 10 THEN SET tax = price * 0.1;");
        
        await _cli.ExecuteCommandAsync("INSERT INTO Item VARIABLES(price: 5);"); // won't fire
        await _cli.ExecuteCommandAsync("INSERT INTO Item VARIABLES(price: 15);"); // will fire

        // 1. Test WITH HAS_FIRED
        var resFired = await _cli.ExecuteCommandAsync("FIND Item WITH HAS_FIRED('CalcTax') RETURN *;");
        Assert.DoesNotContain("[{\"price\":5", resFired!.Content); // Item with price 5 shouldn't be returned
        Assert.Contains("[{\"price\":15", resFired.Content); // Item with price 15 should be returned

        // 2. Test RETURN AUDIT_TRAIL(), GENERATED_VARIABLES()
        var res = await _cli.ExecuteCommandAsync("FIND Item WITH HAS_FIRED('CalcTax') RETURN AUDIT_TRAIL(), GENERATED_VARIABLES(), EXPLAIN_TREE('tax');");
        
        Assert.Contains("CalcTax", res!.Content); // Should contain the log for the rule
        Assert.Contains("AUDIT_TRAIL", res.Content); // Should have the AUDIT_TRAIL field
        Assert.Contains("GENERATED_VARIABLES", res.Content);
        Assert.Contains("tax", res.Content); // tax should be in GENERATED_VARIABLES
        
        // Assert that the Explanation Tree was generated
        Assert.Contains("EXPLAIN_TREE(tax)", res.Content);
        Assert.Contains("Goal", res.Content);
    }

    [Fact]
    public async Task Dump_HospitalDemo()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE HospitalDemo;");
        await _cli.ExecuteCommandAsync("USE HospitalDemo;");
        await _cli.ExecuteCommandAsync(@"
CREATE CONCEPT Patient (
    VARIABLES (
        patientId: STRING,
        name: STRING,
        age: INT,
        sys: DECIMAL, 
        dia: DECIMAL, 
        bmi: DECIMAL,
        riskLevel: STRING
    )
);");
        await _cli.ExecuteCommandAsync("CREATE RULE Rule_HighSys SCOPE Patient p IF p.sys > 140 THEN p.riskLevel = 'High';");
        
        await _cli.ExecuteCommandAsync("INSERT INTO Patient VARIABLES(patientId: 'P01', name: 'Alice', age: 45, sys: 120, dia: 80);");
        await _cli.ExecuteCommandAsync("INSERT INTO Patient VARIABLES(patientId: 'P02', name: 'Bob', age: 60, sys: 150, dia: 90);");
        
        var r1 = await _cli.ExecuteCommandAsync("FIND Patient RETURN *;");
        Console.WriteLine("ALL PATIENTS: " + r1!.Content);
        
        var r2 = await _cli.ExecuteCommandAsync("FIND Patient WITH riskLevel = 'High' RETURN *;");
        Console.WriteLine("HIGH RISK PATIENTS: " + r2!.Content);
        
        Assert.Contains("P02", r2.Content);
        Assert.DoesNotContain("P01", r2.Content);
    }
}
