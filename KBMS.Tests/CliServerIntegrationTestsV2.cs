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
/// Complex integration tests for KBQL V2 syntax and features.
/// </summary>
public class CliServerIntegrationTestsV2 : IAsyncLifetime
{
    private KbmsServer? _server;
    private Cli? _cli;
    private readonly int _testPort;
    private const string TestHost = "localhost";
    private readonly string _testDataDir;

    private static int _nextPort = 34000;
    private static int GetNextPort() => Interlocked.Increment(ref _nextPort);

    public CliServerIntegrationTestsV2()
    {
        _testPort = GetNextPort();
        _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_test_v2_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        if (Directory.Exists(_testDataDir)) Directory.Delete(_testDataDir, true);

        // Start test server
        var storage = new StorageEngine(_testDataDir, "test_v2_key");
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
    public async Task V2_Geometric_ComplexScenario_ShouldSucceed()
    {
        // 1. Setup KB
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE geo_kb;");
        await _cli.ExecuteCommandAsync("USE geo_kb;");

        // 2. KDL: Create Concept with block V2 syntax
        var createQuery = @"
            CREATE CONCEPT Rectangle (
                VARIABLES (
                    id: string,
                    width: double,
                    height: double,
                    area: double,
                    perimeter: double
                )
                CONSTRAINTS (
                    area = width * height,
                    perimeter = 2 * (width + height)
                )
            );";
        var res1 = await _cli.ExecuteCommandAsync(createQuery);
        Assert.Equal(MessageType.RESULT, res1!.Type);

        // 3. KML: Insert
        var insertQuery = "INSERT INTO Rectangle ATTRIBUTE ( id:'R1', width:5.0, height:10.0, area:50.0, perimeter:30.0 );";
        var res2 = await _cli.ExecuteCommandAsync(insertQuery);
        Assert.Equal(MessageType.RESULT, res2!.Type);

        // 4. KQL: SOLVE using SELECT function call
        await _cli.ExecuteCommandAsync("INSERT INTO Rectangle ATTRIBUTE ( id:'R2', width:2.0, height:4.0 );");
        var res3 = await _cli.ExecuteCommandAsync("SELECT SOLVE(area), SOLVE(perimeter) FROM Rectangle WHERE id = 'R2';");
        Assert.Equal(MessageType.RESULT, res3!.Type);
        Assert.Contains("8", res3.Content); // area = 8
        Assert.Contains("12", res3.Content); // perimeter = 12
    }

    [Fact]
    public async Task V2_Transactional_ShadowPaging_ShouldBeAtomic()
    {
        // 1. Setup
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE tx_kb;");
        await _cli.ExecuteCommandAsync("USE tx_kb;");
        var res0 = await _cli.ExecuteCommandAsync("CREATE CONCEPT Counter ( VARIABLES ( val: int ) );");
        Assert.Equal(MessageType.RESULT, res0!.Type);

        // 2. Begin Transaction
        await _cli.ExecuteCommandAsync("BEGIN TRANSACTION;");

        // 3. Insert in Transaction
        await _cli.ExecuteCommandAsync("INSERT INTO Counter ATTRIBUTE (val:100);");
        
        // 4. Verify shadow visibility
        var select1 = await _cli.ExecuteCommandAsync("SELECT * FROM Counter;");
        Assert.Contains("100", select1!.Content);

        // 5. Rollback
        await _cli.ExecuteCommandAsync("ROLLBACK;");

        // 6. Verify empty after rollback
        var select2 = await _cli.ExecuteCommandAsync("SELECT * FROM Counter;");
        Assert.DoesNotContain("100", select2!.Content);
        
        // 7. Successive Commit
        await _cli.ExecuteCommandAsync("BEGIN TRANSACTION;");
        await _cli.ExecuteCommandAsync("INSERT INTO Counter ATTRIBUTE (val:200);");
        await _cli.ExecuteCommandAsync("COMMIT;");
        
        var select3 = await _cli.ExecuteCommandAsync("SELECT * FROM Counter;");
        Assert.Contains("200", select3!.Content);
    }

    [Fact]
    public async Task V2_Inheritance_And_Constraints_Hybrid_ShouldSucceed()
    {
        // 1. Setup
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE hybrid_kb;");
        await _cli.ExecuteCommandAsync("USE hybrid_kb;");

        // 2. Base Concept
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Shape ( VARIABLES (name: string, color: string) );");

        // 3. Circle Concept inheriting Shape
        var createCircle = @"
            CREATE CONCEPT Circle (
                VARIABLES (
                    id: string,
                    radius: double,
                    area: double
                )
            );";
        await _cli.ExecuteCommandAsync(createCircle);
        await _cli.ExecuteCommandAsync("CREATE RULE CalcArea SCOPE Circle IF radius > 0 THEN SET area = 3.14159 * radius * radius;");
        await _cli.ExecuteCommandAsync("ADD HIERARCHY Circle IS_A Shape;");

        // 4. Insert (must respect constraints now!)
        await _cli.ExecuteCommandAsync("INSERT INTO Circle ATTRIBUTE (id:'C1', radius:2.0, area:12.56636);");

        // 5. SOLVE
        await _cli.ExecuteCommandAsync("INSERT INTO Circle ATTRIBUTE (id:'C2', radius:3.0);");
        var res = await _cli.ExecuteCommandAsync("SELECT SOLVE(area) FROM Circle WHERE id = 'C2';");
        Assert.Contains("28", res!.Content); // area = 28.274...
    }
}
