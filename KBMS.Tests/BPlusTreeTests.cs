using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using KBMS.Storage.Core;

namespace KBMS.Tests;

public class BPlusTreeTests : IDisposable
{
    private readonly string _testDir;
    private readonly StoragePool _pool;

    public BPlusTreeTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "kbms_bptree_test_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
        _pool = new StoragePool(_testDir, 64); // 64 frames (very small, forces many evictions)
    }

    public void Dispose()
    {
        _pool.Dispose();
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Fact]
    public void BPlusTree_InsertAndSearch_10000_Entries_ShouldSucceed()
    {
        var bpm = _pool.GetManagers("TestKB").Bpm;
        var tree = new BPlusTree(bpm);

        var keys = new List<Guid>();
        var map = new Dictionary<Guid, RecordId>();

        // Insert 10,000 entries
        for (int i = 0; i < 10000; i++)
        {
            var key = Guid.NewGuid();
            var value = new RecordId(i, i % 10);
            
            keys.Add(key);
            map[key] = value;
            
            tree.Insert(key, value);
        }

        // Verify Search for all 10,000 entries
        foreach (var key in keys)
        {
            var result = tree.Search(key);
            Assert.NotNull(result);
            Assert.Equal(map[key].PageId, result.Value.PageId);
            Assert.Equal(map[key].SlotId, result.Value.SlotId);
        }
        
        // Search for a non-existent key
        var notFound = tree.Search(Guid.NewGuid());
        Assert.Null(notFound);
    }
}
