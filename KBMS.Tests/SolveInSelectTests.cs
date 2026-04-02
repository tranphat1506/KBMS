using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using KBMS.CLI;
using KBMS.Knowledge;
using KBMS.Server;

namespace KBMS.Tests;

public class SolveInSelectTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private KbmsServer _server;
    private Cli _cli;
    private string _dataDir;
    private static int _nextPort = 9300;
    private int _port;

    public SolveInSelectTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _dataDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "kbms_solve_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_dataDir);
        _port = System.Threading.Interlocked.Increment(ref _nextPort);

        _server = new KbmsServer("127.0.0.1", _port, _dataDir);
        _ = _server.StartAsync(); 
        
        _cli = new Cli("127.0.0.1", _port);
        bool connected = false;
        for (int i = 0; i < 20; i++)
        {
            try {
                await _cli.ConnectAsync();
                connected = true;
                break;
            } catch { await Task.Delay(200); }
        }
        if(!connected) throw new Exception("Fixture connect failed");
        await _cli.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE MedicalDB;");
    }

    public async Task DisposeAsync()
    {
        if (_cli != null) await _cli.DisconnectAsync();
        if (_server != null) _server.Stop();
        if (System.IO.Directory.Exists(_dataDir)) 
        {
            try { System.IO.Directory.Delete(_dataDir, true); } catch {}
        }
    }

    [Fact]
    public async Task Test_SolveFunction_InSelectStatement()
    {
        await _cli.ExecuteCommandAsync("USE MedicalDB;");

        // 1. Create Concept
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Patient(name: STRING, age: INT, sys: INT, dia: INT, is_hypertension: BOOLEAN);");

        // 2. Create Rules
        // Rule: If Sys > 140 OR Dia > 90 => Hypertension
        await _cli.ExecuteCommandAsync(
            "CREATE RULE CheckSys SCOPE Patient IF sys >= 140 THEN SET is_hypertension = true;");
        await _cli.ExecuteCommandAsync(
            "CREATE RULE CheckDia SCOPE Patient IF dia >= 90 THEN SET is_hypertension = true;");
        await _cli.ExecuteCommandAsync(
            "CREATE RULE CheckNormal SCOPE Patient IF sys < 140 AND dia < 90 THEN SET is_hypertension = false;");

        // 3. Insert Raw Data
        await _cli.ExecuteCommandAsync("INSERT INTO Patient ATTRIBUTE (name: 'John', age: 45, sys: 120, dia: 80);");
        await _cli.ExecuteCommandAsync("INSERT INTO Patient ATTRIBUTE (name: 'Mary', age: 60, sys: 145, dia: 85);");
        await _cli.ExecuteCommandAsync("INSERT INTO Patient ATTRIBUTE (name: 'Bob', age: 55, sys: 130, dia: 95);");

        // 4. Query with On-the-Fly SOLVE Macro
        var sw = new System.IO.StringWriter();
        var origOut = Console.Out;
        Console.SetOut(sw);

        await _cli.ExecuteCommandAsync("SELECT name, sys, dia, SOLVE(is_hypertension) FROM Patient ORDER BY name;");

        Console.SetOut(origOut);
        var resContent = sw.ToString();
        _output.WriteLine(resContent);

        // The Console.Out will contain an ASCII table:
        // | name | sys | dia | is_hypertension |
        // | John | 120 | 80  | False           |
        Assert.Contains("John", resContent);
        Assert.Contains("false", resContent); // John is False

        Assert.Contains("Mary", resContent);
        Assert.Contains("true", resContent); // Mary is True

        Assert.Contains("Bob", resContent);
        // Bob has dia > 90, so hypertension is True
        Assert.Contains("true", resContent);
    }
}
