using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KBMS.Network;
using KBMS.Server;
using KBMS.Server.V3;
using KBMS.Models.V3;
using Xunit;

namespace KBMS.Tests;

public class DashboardApiTests
{
    [Fact]
    public void ManagementManager_GetStats_ReturnsValidData()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var pool = new KBMS.Storage.V3.StoragePool(tempDir, 16);
        var router = new KBMS.Knowledge.V3.V3DataRouter(pool);

        var cm = new ConnectionManager();
        var sysLogger = new SystemLogger(null!); 
        var mm = new ManagementManager(cm, sysLogger, router, new KBMS.Storage.V3.UserCatalog(pool));

        var stats = mm.GetSystemStats();
        
        Assert.NotNull(stats);
        Assert.True(stats.MemoryMb >= 0);
        Assert.Equal(0, stats.ActiveSessions);
    }

    [Fact]
    public void ManagementManager_ListSessions_ReturnsExistingSessions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var pool = new KBMS.Storage.V3.StoragePool(tempDir, 16);
        var router = new KBMS.Knowledge.V3.V3DataRouter(pool);

        var cm = new ConnectionManager();
        var sysLogger = new SystemLogger(null!);
        var mm = new ManagementManager(cm, sysLogger, router, new KBMS.Storage.V3.UserCatalog(pool));
        
        var clientId = "test_client";
        var session = cm.CreateSession(clientId, null!, "127.0.0.1");
        session.IpAddress = "127.0.0.1";
        session.User = new KBMS.Models.User { Username = "admin", Role = KBMS.Models.UserRole.ROOT };

        var sessions = mm.ListSessions();

        Assert.Single(sessions);
    }

    [Fact]
    public async Task ManagementManager_BroadcastLog_SendsToSubscribers()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var pool = new KBMS.Storage.V3.StoragePool(tempDir, 16);
        var router = new KBMS.Knowledge.V3.V3DataRouter(pool);

        var cm = new ConnectionManager();
        var sysLogger = new SystemLogger(null!);
        var mm = new ManagementManager(cm, sysLogger, router, new KBMS.Storage.V3.UserCatalog(pool));
        
        using var ms = new MemoryStream();
        mm.SubscribeToLogs("client1", ms);

        // This will trigger BroadcastLog through the event
        sysLogger.Info("System", "Test message");

        // Wait a bit for async broadcast
        await Task.Delay(500);

        ms.Position = 0;
        Assert.True(ms.Length > 0);
        
        // Protocol verification would need message reading logic but ms.Length > 0 confirms data was written
    }
}
