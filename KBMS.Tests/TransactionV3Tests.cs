using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using KBMS.Storage.V3;
using KBMS.Models;

namespace KBMS.Tests;

/// <summary>
/// Layer 4: Transaction Control + WAL V3 Tests
/// Verifies WAL-based begin/commit/rollback and crash recovery.
/// </summary>
public class TransactionV3Tests : IDisposable
{
    private readonly string _dbPath;
    private readonly DiskManager _disk;
    private readonly BufferPoolManager _bpm;
    private readonly WalManagerV3 _wal;

    public TransactionV3Tests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".kdb");
        _disk = new DiskManager(_dbPath);
        _wal = new WalManagerV3(_dbPath);
        _bpm = new BufferPoolManager(_disk, _wal, 32);
    }

    public void Dispose()
    {
        _bpm?.Dispose();
        _wal?.Dispose();
        if (File.Exists(_dbPath)) try { File.Delete(_dbPath); } catch {}
        if (File.Exists(_dbPath + ".wal")) try { File.Delete(_dbPath + ".wal"); } catch {}
    }

    // Test 1: Begin + Commit - WAL records the transaction
    [Fact]
    public void Begin_And_Commit_Succeeds()
    {
        var txnId = _wal.Begin();
        Assert.True(_wal.IsTransactionActive(txnId));

        var before = new byte[16];
        var after = new byte[16];
        after[0] = 0xFF;

        _wal.LogWrite(txnId, pageId: 1, beforeImage: before, afterImage: after);
        _wal.Commit(txnId);

        Assert.False(_wal.IsTransactionActive(txnId));
    }

    // Test 2: Rollback returns before-images
    [Fact]
    public void Rollback_Returns_BeforeImages()
    {
        var txnId = _wal.Begin();

        var before = new byte[16]; before[3] = 0xAB;
        var after = new byte[16];  after[3] = 0xFF;

        _wal.LogWrite(txnId, pageId: 5, beforeImage: before, afterImage: after);

        var restorations = _wal.Rollback(txnId);

        Assert.Single(restorations);
        Assert.Equal(5, restorations[0].pageId);
        Assert.Equal(0xAB, restorations[0].beforeImage[3]); // Original value preserved
        Assert.False(_wal.IsTransactionActive(txnId));
    }

    // Test 3: After rollback, original page data is correct
    [Fact]
    public void Rollback_RestoresOriginalValue()
    {
        int pageId = _disk.AllocatePage();
        var page = _bpm.FetchPage(pageId);
        Assert.NotNull(page);

        byte[] beforeSnapshot = new byte[page!.Data.Length];
        page.Data.CopyTo(beforeSnapshot, 0);

        // "Modify" the page
        page.Data[0] = 0xFF;
        _bpm.UnpinPage(pageId, true);

        var txnId = _wal.Begin();
        _wal.LogWrite(txnId, pageId, beforeSnapshot, page.Data);
        var restorations = _wal.Rollback(txnId);

        // Apply rollback: restore before-image
        var restoredPage = _bpm.FetchPage(pageId);
        Assert.NotNull(restoredPage);
        restorations[0].beforeImage.CopyTo(restoredPage!.Data, 0);
        _bpm.UnpinPage(pageId, true);

        var verifyPage = _bpm.FetchPage(pageId);
        Assert.Equal(0x00, verifyPage!.Data[0]); // Restored to original
        _bpm.UnpinPage(pageId, false);
    }

    // Test 4: WAL crash recovery finds uncommitted entries
    [Fact]
    public void WAL_Recovery_FindsUncommittedEntries()
    {
        // Simulate a crashed transaction: log but don't commit
        var txnId = _wal.Begin();
        _wal.LogWrite(txnId, pageId: 99, beforeImage: new byte[8], afterImage: new byte[8]);
        // No commit! _wal.Commit(txnId) NOT called → simulates crash

        // Create a fresh WAL reader pointing to the same file
        using var recoveryWal = new WalManagerV3(_dbPath);
        var uncommitted = recoveryWal.RecoverUncommitted();

        Assert.NotEmpty(uncommitted);
        Assert.Equal(99, uncommitted[0].pageId);
    }

    // Test 5: Nested transactions should not conflict
    [Fact]
    public void Multiple_Concurrent_Transactions_Tracked_Separately()
    {
        var txn1 = _wal.Begin();
        var txn2 = _wal.Begin();

        Assert.NotEqual(txn1, txn2);
        Assert.True(_wal.IsTransactionActive(txn1));
        Assert.True(_wal.IsTransactionActive(txn2));

        _wal.LogWrite(txn1, pageId: 1, new byte[4], new byte[4]);
        _wal.LogWrite(txn2, pageId: 2, new byte[4], new byte[4]);

        _wal.Commit(txn1);
        Assert.False(_wal.IsTransactionActive(txn1));
        Assert.True(_wal.IsTransactionActive(txn2)); // txn2 still open

        _wal.Rollback(txn2);
        Assert.False(_wal.IsTransactionActive(txn2));
    }
}
