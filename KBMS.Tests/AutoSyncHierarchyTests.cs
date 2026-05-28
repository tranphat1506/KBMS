using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using KBMS.CLI;
using KBMS.Server;
using KBMS.Storage;

namespace KBMS.Tests;

public class AutoSyncHierarchyTests : IDisposable
{
    private readonly Cli _cli;
    private readonly KbmsServer _server;
    private readonly string _testDataDir;
    private readonly string _testKb = "SyncTestKb";

    public AutoSyncHierarchyTests()
    {
        _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_test_{Guid.NewGuid():N}");
        var storage = new StorageEngine(_testDataDir, "test_encryption_key");
        _server = new KbmsServer("localhost", 33010, _testDataDir);
        _ = _server.StartAsync();
        
        Task.Delay(1000).Wait();
        
        _cli = new Cli("localhost", 33010);
        _cli.ConnectAsync(false).Wait();
        
        _cli.ExecuteCommandAsync("LOGIN root root;").Wait();
        _cli.ExecuteCommandAsync($"CREATE KNOWLEDGE BASE {_testKb};").Wait();
        _cli.ExecuteCommandAsync($"USE {_testKb};").Wait();
    }

    public void Dispose()
    {
        _cli.ExecuteCommandAsync($"DROP KNOWLEDGE BASE {_testKb};").Wait();
        _cli.DisconnectAsync().Wait();
        _server.Stop();
        if (Directory.Exists(_testDataDir))
            Directory.Delete(_testDataDir, true);
    }

    [Fact]
    public async Task CreateConcept_WithInherits_ShouldAutoAddHierarchy()
    {
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Person ( VARIABLES (name: STRING) );");
        var res = await _cli.ExecuteCommandAsync("CREATE CONCEPT Student ( BASE_OBJECTS (Person) VARIABLES (studentId: STRING) );");
        Assert.Contains("\"success\":true", res.Content, StringComparison.OrdinalIgnoreCase);
        
        var describeRes = await _cli.ExecuteCommandAsync($"SHOW HIERARCHIES;");
        Assert.Contains("Student", describeRes.Content);
    }

    [Fact]
    public async Task AddHierarchy_ShouldAutoAddBaseObject()
    {
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Animal ( VARIABLES (species: STRING) );");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Dog ( VARIABLES (breed: STRING) );");
        
        var res = await _cli.ExecuteCommandAsync("ADD HIERARCHY Dog IS_A Animal;");
        Assert.Contains("success", res.Content, StringComparison.OrdinalIgnoreCase);
        
        var describeRes = await _cli.ExecuteCommandAsync("DESCRIBE CONCEPT Dog;");
        Assert.Contains("Animal", describeRes.Content);
    }

    [Fact]
    public async Task RemoveHierarchy_ShouldAutoRemoveBaseObject()
    {
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Animal ( VARIABLES (species: STRING) );");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Dog ( VARIABLES (breed: STRING) );");
        await _cli.ExecuteCommandAsync("ADD HIERARCHY Dog IS_A Animal;");
        
        var res = await _cli.ExecuteCommandAsync("REMOVE HIERARCHY Animal IS_A Dog;"); 
        Assert.Contains("success", res.Content, StringComparison.OrdinalIgnoreCase);
        
        var describeRes = await _cli.ExecuteCommandAsync("DESCRIBE CONCEPT Dog;");
        Assert.DoesNotContain("\"BaseObjects\":\"Animal\"", describeRes.Content.Replace(" ", "")); 
    }
}
