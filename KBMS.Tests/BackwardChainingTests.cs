using KBMS.Parser.Ast.Kql;
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

public class BackwardChainingTests : IAsyncLifetime
{
    private Server.KbmsServer? _server;
    private Cli? _cli;
    private readonly int _testPort;
    private readonly string _testDataDir;

    private static int _nextPort = 36000;
    private static int GetNextPort() => Interlocked.Increment(ref _nextPort);

    public BackwardChainingTests()
    {
        _testPort = GetNextPort();
        _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_backward_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        if (Directory.Exists(_testDataDir)) Directory.Delete(_testDataDir, true);
        var storage = new Storage.StorageEngine(_testDataDir, "backward_key");
        _server = new Server.KbmsServer("localhost", _testPort, storage);
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
    public async Task Solve_RecursiveBackwardChaining_ShouldWork()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE back_kb;");
        await _cli.ExecuteCommandAsync("USE back_kb;");
        
        await _cli.ExecuteCommandAsync(@"
            CREATE CONCEPT Student (
                VARIABLES (
                    id: INT,
                    grade: DECIMAL,
                    honor: STRING,
                    gifted: BOOLEAN
                )
            );");

        // Rule 1: grade -> honor
        await _cli.ExecuteCommandAsync("CREATE RULE R1 SCOPE Student IF grade >= 90 THEN SET honor = 'High';");
        
        // Rule 2: honor -> gifted (Recursive)
        await _cli.ExecuteCommandAsync("CREATE RULE R2 SCOPE Student IF honor = 'High' THEN SET gifted = true;");

        // Test SOLVE: Starting from grade, find gifted (2-step recursive goal)
        var res = await _cli.ExecuteCommandAsync("SOLVE ON CONCEPT Student GIVEN grade: 95 FIND gifted;");
        
        Assert.Equal(MessageType.RESULT, res!.Type);
        Assert.Contains("Derived Fact: gifted = True", res.Content);
        Assert.Contains("Rule R1 resolved honor", res.Content);
        Assert.Contains("Rule R2 resolved gifted", res.Content);
    }

    [Fact]
    public async Task Solve_BackwardChaining_PartialMatch_ShouldFailGracefully()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE back_fail_kb;");
        await _cli.ExecuteCommandAsync("USE back_fail_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Student ( VARIABLES (grade: DECIMAL, gifted: BOOLEAN) );");
        await _cli.ExecuteCommandAsync("CREATE RULE R1 SCOPE Student IF grade >= 90 THEN SET gifted = true;");

        // Goal cannot be met because grade is too low
        var res = await _cli.ExecuteCommandAsync("SOLVE ON CONCEPT Student GIVEN grade: 80 FIND gifted;");
        
        Assert.Equal(MessageType.RESULT, res!.Type);
        Assert.Contains("Could not resolve goals: gifted", res.Content);
    }

    [Fact]
    public async Task Solve_BackwardChaining_CircularDependency_ShouldNotInfiniteLoop()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE circle_kb;");
        await _cli.ExecuteCommandAsync("USE circle_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Node ( VARIABLES (a: BOOLEAN, b: BOOLEAN) );");

        // Circular rules
        await _cli.ExecuteCommandAsync("CREATE RULE R_A_to_B SCOPE Node IF a = true THEN SET b = true;");
        await _cli.ExecuteCommandAsync("CREATE RULE R_B_to_A SCOPE Node IF b = true THEN SET a = true;");

        var res = await _cli.ExecuteCommandAsync("SOLVE ON CONCEPT Node GIVEN a: true FIND b;");
        // Should resolve because a=true is given
        Assert.Contains("Derived Fact: b = True", res!.Content);

        // Goal that is impossible and circular (none known)
        var res2 = await _cli.ExecuteCommandAsync("SOLVE ON CONCEPT Node FIND b;");
        Assert.Contains("Circular dependency: b", res2!.Content);
    }
}
