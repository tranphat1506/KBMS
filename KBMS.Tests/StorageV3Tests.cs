using System;
using System.IO;
using Xunit;
using KBMS.Storage.V3;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Tests;

public class StorageV3Tests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DiskManager _diskManager;
    private readonly BufferPoolManager _bpm;

    public StorageV3Tests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"kbms_v3_test_{Guid.NewGuid()}.db");
        _diskManager = new DiskManager(_testDbPath);
        _bpm = new BufferPoolManager(_diskManager, poolSize: 5);
    }

    public void Dispose()
    {
        _bpm.Dispose();
        _diskManager.Dispose();
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }

    [Fact]
    public void DiskManager_AllocateAndReadWritePage_Success()
    {
        int pageId = _diskManager.AllocatePage();
        Assert.Equal(0, pageId);

        var page = new Page { PageId = pageId };
        
        // Write some data
        page.Data[0] = 42;
        page.Data[16383] = 99;
        
        _diskManager.WritePage(pageId, page);

        var readPage = new Page();
        _diskManager.ReadPage(pageId, readPage);

        Assert.Equal(42, readPage.Data[0]);
        Assert.Equal(99, readPage.Data[16383]);
    }

    [Fact]
    public void BufferPoolManager_LRUEviction_Works()
    {
        // Pool size is 5
        for (int i = 0; i < 6; i++)
        {
            var p = _bpm.NewPage(out int pid);
            Assert.NotNull(p);
            // Must unpin to allow eviction
            _bpm.UnpinPage(pid, isDirty: true);
        }

        // The 0th page should have been evicted to disk and replaced by page 5
        var fetchedPage = _bpm.FetchPage(0);
        Assert.NotNull(fetchedPage);
        Assert.Equal(0, fetchedPage.PageId);
        _bpm.UnpinPage(0, false);
    }

    [Fact]
    public void SlottedPage_InsertAndRetrieveTuple_Success()
    {
        var page = _bpm.NewPage(out int pageId);
        Assert.NotNull(page);

        var slottedPage = new SlottedPage(page);
        slottedPage.Init(pageId);

        var tuple = new Tuple();
        tuple.AddInt(12345);
        tuple.AddString("Hello KBMS V3");
        tuple.AddBool(true);

        byte[] tupleData = tuple.Serialize();

        int slotId = slottedPage.InsertTuple(tupleData);
        Assert.Equal(0, slotId);

        byte[] retrievedData = slottedPage.GetTuple(slotId)!;
        Assert.NotNull(retrievedData);

        var retrievedTuple = Tuple.Deserialize(retrievedData);
        Assert.Equal(12345, retrievedTuple.GetInt(0));
        Assert.Equal("Hello KBMS V3", retrievedTuple.GetString(1));
        Assert.True(retrievedTuple.GetBool(2));

        _bpm.UnpinPage(pageId, true);
    }
    
    [Fact]
    public void SlottedPage_DeleteTuple_ReusesSlot()
    {
        var page = _bpm.NewPage(out int pageId);
        var slottedPage = new SlottedPage(page);
        slottedPage.Init(pageId);
        
        var tuple1 = new Tuple(); tuple1.AddInt(1);
        var tuple2 = new Tuple(); tuple2.AddInt(2);
        
        int slot0 = slottedPage.InsertTuple(tuple1.Serialize());
        int slot1 = slottedPage.InsertTuple(tuple2.Serialize());
        
        Assert.Equal(0, slot0);
        Assert.Equal(1, slot1);
        
        // Delete first tuple
        bool deleted = slottedPage.DeleteTuple(slot0);
        Assert.True(deleted);
        
        // Insert third tuple, should reuse slot 0
        var tuple3 = new Tuple(); tuple3.AddInt(3);
        int slotNew = slottedPage.InsertTuple(tuple3.Serialize());
        Assert.Equal(0, slotNew);
        
        _bpm.UnpinPage(pageId, true);
    }
}
