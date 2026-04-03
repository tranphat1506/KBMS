using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KBMS.Storage.V3;

/// <summary>
/// V3 Write-Ahead Log Manager.
/// Implements the standard WAL protocol: every page write is logged BEFORE the page is flushed to disk.
/// On crash, replay uncommitted before-images to restore consistency.
///
/// Log Entry Format (binary):
///   [TxnId          - 16 bytes Guid ]
///   [PageId         -  4 bytes int  ]
///   [BeforeLen      -  4 bytes int  ]
///   [BeforeImage    - N bytes       ]
///   [AfterLen       -  4 bytes int  ]
///   [AfterImage     - N bytes       ]
///   [Committed flag -  1 byte bool  ]
/// </summary>
public class WalManagerV3 : IDisposable
{
    private readonly string _walPath;
    private readonly FileStream _walFile;
    private readonly object _writeLock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _syncTask;

    // In-memory index: txnId -> list of (offset, pageId) in the WAL file
    private readonly ConcurrentDictionary<Guid, List<long>> _activeTxns = new();

    public WalManagerV3(string dbPath)
    {
        _walPath = dbPath + ".wal";
        // Use a 128KB buffer for the FileStream to minimize syscalls
        _walFile = new FileStream(_walPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 128 * 1024);
        _walFile.Seek(0, SeekOrigin.End);

        // 1-second Heartbeat Sync
        _syncTask = Task.Run(PeriodicSyncAsync);
    }

    private async Task PeriodicSyncAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, _cts.Token);
                Flush(true);
            }
            catch { break; }
        }
    }

    public void Flush(bool physicalDiskSync = false)
    {
        lock (_writeLock)
        {
            try { _walFile.Flush(physicalDiskSync); } catch { }
        }
    }

    // ===================== BEGIN =====================

    public Guid Begin()
    {
        var txnId = Guid.NewGuid();
        _activeTxns[txnId] = new List<long>();
        return txnId;
    }

    // ===================== LOG INSERT (Row-Level) =====================
    /// <summary>
    /// Records a specific TUPLE insertion. [Type:4] [TxnId:16] [PageId:4] [SlotId:4] [DataLen:4] [Data:N]
    /// </summary>
    public void LogInsert(Guid txnId, int pageId, int slotId, byte[] data)
    {
        if (!_activeTxns.ContainsKey(txnId))
            _activeTxns[txnId] = new List<long>();

        lock (_writeLock)
        {
            _walFile.Seek(0, SeekOrigin.End);
            long entryOffset = _walFile.Position;
            using var bw = new BinaryWriter(_walFile, Encoding.UTF8, leaveOpen: true);
            bw.Write((byte)4);                        // Type: ROW_INSERT
            bw.Write(txnId.ToByteArray());           // 16 bytes
            bw.Write(pageId);                         //  4 bytes
            bw.Write(slotId);                         //  4 bytes
            bw.Write(data.Length);                    //  4 bytes
            bw.Write(data);                           //  N bytes
            _activeTxns[txnId].Add(entryOffset);
        }
    }

    // ===================== LOG WRITE (Full Page Diff) =====================
    /// <summary>
    /// Records a write operation: both the before-image and after-image.
    /// Restored for compatibility with catalog components.
    /// </summary>
    public void LogWrite(Guid txnId, int pageId, byte[] beforeImage, byte[] afterImage)
    {
        if (!_activeTxns.ContainsKey(txnId))
            _activeTxns[txnId] = new List<long>();

        lock (_writeLock)
        {
            _walFile.Seek(0, SeekOrigin.End);
            long entryOffset = _walFile.Position;
            using var bw = new BinaryWriter(_walFile, Encoding.UTF8, leaveOpen: true);
            bw.Write((byte)2);                        // Type: PAGE_WRITE
            bw.Write(txnId.ToByteArray());           // 16 bytes
            bw.Write(pageId);                         //  4 bytes
            bw.Write(beforeImage.Length);             //  4 bytes
            bw.Write(beforeImage);                    //  N bytes
            bw.Write(afterImage.Length);              //  4 bytes
            bw.Write(afterImage);                     //  N bytes
            bw.Write(false);                          //  1 byte (not yet committed)
            _activeTxns[txnId].Add(entryOffset);
        }
    }

    /// <summary>
    /// RECORDS A FULL PAGE IMAGE directly into the WAL .wal file.
    /// This is the "Full Page Logging" fast-path for dirty page evictions.
    /// </summary>
    public void LogFullPage(int pageId, byte[] data)
    {
        lock (_writeLock)
        {
            _walFile.Seek(0, SeekOrigin.End);
            using var bw = new BinaryWriter(_walFile, Encoding.UTF8, leaveOpen: true);
            bw.Write((byte)3);                        // Type: FULL_PAGE_IMAGE
            bw.Write(pageId);                         // 4 bytes
            bw.Write(data.Length);                    // 4 bytes
            bw.Write(data);                           // N bytes
        }
    }

    // ===================== COMMIT =====================

    /// <summary>
    /// Marks all entries for this transaction as committed (safe to evict from WAL).
    /// </summary>
    public void Commit(Guid txnId)
    {
        if (!_activeTxns.TryRemove(txnId, out var offsets)) return;

        lock (_writeLock)
        {
            using var bw = new BinaryWriter(_walFile, Encoding.UTF8, leaveOpen: true);
            foreach (var entryOffset in offsets)
            {
                // Seek-back to update the committed flag
                _walFile.Seek(entryOffset, SeekOrigin.Begin);
                using var br = new BinaryReader(_walFile, Encoding.UTF8, leaveOpen: true);
                
                byte type = br.ReadByte();
                if (type != 2) continue; // Only PAGE_WRITE entries have committed flags
                
                br.ReadBytes(16); // TxnId
                br.ReadInt32();   // PageId
                int beforeLen = br.ReadInt32();
                _walFile.Seek(beforeLen, SeekOrigin.Current);
                int afterLen = br.ReadInt32();
                _walFile.Seek(afterLen, SeekOrigin.Current);
                
                long flagPosition = _walFile.Position;
                _walFile.Seek(flagPosition, SeekOrigin.Begin);
                bw.Write(true);
            }
            _walFile.Seek(0, SeekOrigin.End);
        }
    }

    // ===================== ROLLBACK =====================

    /// <summary>
    /// Replays before-images for all uncommitted entries of this transaction.
    /// </summary>
    public List<(int pageId, byte[] beforeImage)> Rollback(Guid txnId)
    {
        var restorations = new List<(int, byte[])>();

        if (!_activeTxns.TryRemove(txnId, out var offsets)) return restorations;

        lock (_writeLock)
        {
            foreach (var entryOffset in offsets)
            {
                _walFile.Seek(entryOffset, SeekOrigin.Begin);
                using var br = new BinaryReader(_walFile, Encoding.UTF8, leaveOpen: true);
                br.ReadBytes(16); // TxnId
                int pageId = br.ReadInt32();
                int beforeLen = br.ReadInt32();
                byte[] beforeImage = br.ReadBytes(beforeLen);

                restorations.Add((pageId, beforeImage));
            }
            _walFile.Seek(0, SeekOrigin.End);
        }

        return restorations;
    }

    // ===================== CRASH RECOVERY =====================

    /// <summary>
    /// Reads the WAL log and returns all UNCOMMITTED entries.
    /// The caller (BufferPoolManager) should apply the before-images back to disk.
    /// </summary>
    public List<(Guid txnId, int pageId, byte[] beforeImage)> RecoverUncommitted()
    {
        var results = new List<(Guid, int, byte[])>();
        _walFile.Seek(0, SeekOrigin.Begin);

        using var br = new BinaryReader(_walFile, Encoding.UTF8, leaveOpen: true);
        while (_walFile.Position < _walFile.Length)
        {
            try
            {
                var txnId = new Guid(br.ReadBytes(16));
                int pageId = br.ReadInt32();
                int beforeLen = br.ReadInt32();
                byte[] before = br.ReadBytes(beforeLen);
                int afterLen = br.ReadInt32();
                br.ReadBytes(afterLen); // skip after-image
                bool committed = br.ReadBoolean();

                if (!committed)
                    results.Add((txnId, pageId, before));
            }
            catch
            {
                break; // Partial write at end of WAL is normal after crash
            }
        }

        _walFile.Seek(0, SeekOrigin.End);
        return results;
    }

    public bool IsTransactionActive(Guid txnId) => _activeTxns.ContainsKey(txnId);

    public void Dispose()
    {
        _cts.Cancel();
        try { _syncTask.Wait(2000); } catch { }
        
        Flush(true); // Final physical sync
        _walFile?.Close();
        _walFile?.Dispose();
    }
}
