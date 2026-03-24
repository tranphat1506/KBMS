using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using KBMS.Storage.V3;
using KBMS.Models;
using KBMS.Knowledge.V3;
using KBMS.Server.V3;

namespace KBMS.Tests;

public class Phase4UpdateTests : IDisposable
{
    private readonly string _tempDir;
    private readonly StoragePool _storagePool;
    private readonly KbCatalog _kbCatalog;
    private readonly ConceptCatalog _conceptCatalog;
    private readonly UserCatalog _userCatalog;
    private readonly V3DataRouter _router;

    public Phase4UpdateTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "KBMS_Phase4_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _storagePool = new StoragePool(_tempDir, 32);
        _kbCatalog = new KbCatalog(_storagePool);
        
        // Ensure system KB exists for the updater
        _kbCatalog.CreateKb("system", Guid.NewGuid());
        _conceptCatalog = new ConceptCatalog(_storagePool);
        _userCatalog = new UserCatalog(_storagePool);
        _router = new V3DataRouter(_storagePool);
    }

    public void Dispose()
    {
        _storagePool.Dispose();
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Test_SystemUpdate_FromBaselineToLatest()
    {
        // 1. Setup Baseline (3.1.0)
        _router.InsertObject("system", new ObjectInstance 
        { 
            ConceptName = "settings", 
            Values = new Dictionary<string, object> 
            { 
                ["variable_name"] = "EngineVersion", 
                ["variable_value"] = "3.1.0" 
            } 
        });

        // 2. Run Updater
        SystemUpdater.Run(_kbCatalog, _conceptCatalog, _userCatalog, _router);

        // 3. Verify Version Persistence
        var versionSetting = _router.SelectObjects("system", "settings", v => v["variable_name"]?.ToString() == "EngineVersion").FirstOrDefault();
        Assert.NotNull(versionSetting);
        Assert.Equal("3.3.0-multi-db", versionSetting.Values["variable_value"]?.ToString());

        // 4. Verify Log Generation
        var logs = _router.SelectObjects("system", "system_logs");
        Assert.NotEmpty(logs);
        
        // Should have a log for 3.2.0-beta and 3.3.0-multi-db
        var log32 = logs.Any(l => l.Values["message"]?.ToString()?.Contains("3.2.0-beta") == true);
        var log33 = logs.Any(l => l.Values["message"]?.ToString()?.Contains("3.3.0-multi-db") == true);
        
        Assert.True(log32, "Missing log for v3.2 migration");
        Assert.True(log33, "Missing log for v3.3 migration");

        // 5. Verify Root User Creation
        var root = _userCatalog.FindUser("root");
        Assert.NotNull(root);
        Assert.Equal(UserRole.ROOT, root.Role);
    }

    [Fact]
    public void Test_SystemUpdate_VersionJumpingHandlesAlreadyUpdated()
    {
        // 1. Setup Latest Version
        _router.InsertObject("system", new ObjectInstance 
        { 
            ConceptName = "settings", 
            Values = new Dictionary<string, object> 
            { 
                ["variable_name"] = "EngineVersion", 
                ["variable_value"] = "3.3.0-multi-db" 
            } 
        });

        // 2. Run Updater again
        SystemUpdater.Run(_kbCatalog, _conceptCatalog, _userCatalog, _router);

        // 3. Verify it stayed at latest
        var versionSetting = _router.SelectObjects("system", "settings", v => v["variable_name"]?.ToString() == "EngineVersion").FirstOrDefault();
        Assert.Equal("3.3.0-multi-db", versionSetting.Values["variable_value"]?.ToString());
    }
}
