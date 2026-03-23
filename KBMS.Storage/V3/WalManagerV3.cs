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

    // In-memory index: txnId -> list of (offset, pageId) in the WAL file
    private readonly ConcurrentDictionary<Guid, List<long>> _activeTxns = new();

    public WalManagerV3(string dbPath)
    {
        _walPath = dbPath + ".wal";
        _walFile = new FileStream(_walPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        _walFile.Seek(0, SeekOrigin.End); // Always append
    }

    // ===================== BEGIN =====================

    public Guid Begin()
    {
        var txnId = Guid.NewGuid();
        _activeTxns[txnId] = new List<long>();
        return txnId;
    }

    // ===================== LOG WRITE =====================

    /// <summary>
    /// Records a write operation: both the before-image (for rollback) and after-image (for redo).
    /// Must be called BEFORE the page is flushed to disk.
    /// </summary>
    public void LogWrite(Guid txnId, int pageId, byte[] beforeImage, byte[] afterImage)
    {
        if (!_activeTxns.ContainsKey(txnId))
            throw new InvalidOperationException($"Transaction {txnId} not active.");

        lock (_writeLock)
        {
            long entryOffset = _walFile.Position;

            using var bw = new BinaryWriter(_walFile, Encoding.UTF8, leaveOpen: true);
            bw.Write(txnId.ToByteArray());           // 16 bytes
            bw.Write(pageId);                         //  4 bytes
            bw.Write(beforeImage.Length);             //  4 bytes
            bw.Write(beforeImage);                    //  N bytes
            bw.Write(afterImage.Length);              //  4 bytes
            bw.Write(afterImage);                     //  N bytes
            bw.Write(false);                          //  1 byte (not yet committed)
            _walFile.Flush();

            _activeTxns[txnId].Add(entryOffset);
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
                // Calculate offset of the committed flag within this entry:
                // 16 (TxnId) + 4 (PageId) + 4 (BeforeLen) + BeforeLen + 4 (AfterLen) + AfterLen
                // We do a seek-back to update the flag
                _walFile.Seek(entryOffset, SeekOrigin.Begin);
                using var br = new BinaryReader(_walFile, Encoding.UTF8, leaveOpen: true);
                br.ReadBytes(16); // TxnId
                br.ReadInt32();   // PageId
                int beforeLen = br.ReadInt32();
                br.ReadBytes(beforeLen);
                int afterLen = br.ReadInt32();
                br.ReadBytes(afterLen);
                long flagPosition = _walFile.Position;

                _walFile.Seek(flagPosition, SeekOrigin.Begin);
                bw.Write(true);
            }
            _walFile.Seek(0, SeekOrigin.End); // Reset to end for next append
            _walFile.Flush();
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
        _walFile?.Close();
        _walFile?.Dispose();
    }
}
