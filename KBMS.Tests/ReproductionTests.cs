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

public class ReproductionTests : IAsyncLifetime
{
    private Server.KbmsServer? _server;
    private Cli? _cli;
    private readonly int _testPort;
    private readonly string _testDataDir;

    private static int _nextPort = 38000;
    private static int GetNextPort() => Interlocked.Increment(ref _nextPort);

    public ReproductionTests()
    {
        _testPort = GetNextPort();
        _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_repro_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        if (Directory.Exists(_testDataDir)) Directory.Delete(_testDataDir, true);
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
    public async Task TestCalculations_ShouldNotReturnNull()
    {
        await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE ReproKB;");
        await _cli.ExecuteCommandAsync("USE ReproKB;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Product (VARIABLES(id: INT, price: DECIMAL, stock: INT));");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE (1, 100, 10);");

        // 1. Simple expression with alias
        var res1 = await _cli.ExecuteCommandAsync("SELECT p.price * 1.1 AS Result FROM Product p;");
        Assert.NotNull(res1);
        if (res1.Type == MessageType.ERROR) throw new Exception($"Query 1 failed: {res1.Content}");
        Assert.Contains("110", res1.Content);
        Assert.DoesNotContain("null", res1.Content.ToLower());

        // 2. Simple expression without alias
        var res2 = await _cli.ExecuteCommandAsync("SELECT price * 1.1 AS Result FROM Product;");
        Assert.NotNull(res2);
        if (res2.Type == MessageType.ERROR) throw new Exception($"Query 2 failed: {res2.Content}");
        Assert.Contains("110", res2.Content);
        Assert.DoesNotContain("null", res2.Content.ToLower());

        // 3. Multiplication of two fields
        var res3 = await _cli.ExecuteCommandAsync("SELECT price * stock AS TotalValue FROM Product;");
        Assert.NotNull(res3);
        if (res3.Type == MessageType.ERROR) throw new Exception($"Query 3 failed: {res3.Content}");
        Assert.Contains("1000", res3.Content);
        Assert.DoesNotContain("null", res3.Content.ToLower());

        // 4. Expression with function
        var res4 = await _cli.ExecuteCommandAsync("SELECT Sqrt(price) AS SqrtPrice FROM Product;");
        Assert.NotNull(res4);
        if (res4.Type == MessageType.ERROR) throw new Exception($"Query 4 failed: {res4.Content}");
        Assert.Contains("10", res4.Content);
        Assert.DoesNotContain("null", res4.Content.ToLower());
        
        // 5. Expression with literal solo
        var res5 = await _cli.ExecuteCommandAsync("SELECT 1 + 2 AS Three FROM Product;");
        Assert.NotNull(res5);
        if (res5.Type == MessageType.ERROR) throw new Exception($"Query 5 failed: {res5.Content}");
        Assert.Contains("3", res5.Content);
        Assert.DoesNotContain("null", res5.Content.ToLower());

        // 6. CALC() function
        var res6 = await _cli.ExecuteCommandAsync("SELECT CALC(p.price * 1.5) AS CalcResult FROM Product p;");
        Assert.NotNull(res6);
        if (res6.Type == MessageType.ERROR) throw new Exception($"Query 6 failed: {res6.Content}");
        Assert.Contains("150", res6.Content);
        Assert.DoesNotContain("null", res6.Content.ToLower());

        // 7. JOIN with shorthand alias and newlines (from full_test.kbql)
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Dept (VARIABLES(id: INT, name: STRING));");
        await _cli.ExecuteCommandAsync("INSERT INTO Dept ATTRIBUTE (1, 'IT');");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Emp (VARIABLES(id: INT, name: STRING, dept_id: INT));");
        await _cli.ExecuteCommandAsync("INSERT INTO Emp ATTRIBUTE (1, 'Alice', 1);");

        var joinQuery = @"
SELECT 
    e.name AS EmployeeName, 
    d.name AS DepartmentName
FROM Emp e 
JOIN Dept d ON e.dept_id = d.id
WHERE e.id = 1;";
        var res7 = await _cli.ExecuteCommandAsync(joinQuery);
        Assert.NotNull(res7);
        if (res7.Type == MessageType.ERROR) throw new Exception($"Query 7 failed: {res7.Content}");
        Assert.Contains("Alice", res7.Content);
        Assert.Contains("IT", res7.Content);
    }
}
