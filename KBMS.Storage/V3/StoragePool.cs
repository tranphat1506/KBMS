using System;
using System.Collections.Generic;
using System.IO;

namespace KBMS.Storage.V3;

/// <summary>
/// StoragePool manages multiple DiskManager and BufferPoolManager instances
/// for a Multi-DB (File-per-KB) architecture. It ensures that each KB has its
/// own physical .kdb file while providing a unified access interface.
/// </summary>
public class StoragePool : IDisposable
{
    private readonly string _dataDir;
    private readonly int _defaultPoolSize;
    private readonly string _masterKey;
    private readonly Dictionary<string, (DiskManager Disk, BufferPoolManager Bpm, WalManagerV3 Wal)> _pools = new();
    private readonly object _lock = new();

    public StoragePool(string dataDir, int defaultPoolSize = 100, string masterKey = "KBMS_V3_MASTER_SECRET_2026")
    {
        _dataDir = dataDir;
        _defaultPoolSize = defaultPoolSize;
        _masterKey = masterKey;
        if (!Directory.Exists(_dataDir)) Directory.CreateDirectory(_dataDir);
    }

    /// <summary>
    /// Gets the DiskManager, BufferPoolManager, and WalManagerV3 for a given Knowledge Base.
    /// If the KB is not yet loaded, it initializes its storage components.
    /// </summary>
    public (DiskManager Disk, BufferPoolManager Bpm, WalManagerV3 Wal) GetManagers(string kbName)
    {
        lock (_lock)
        {
            if (_pools.TryGetValue(kbName, out var managers))
            {
                return managers;
            }

            // Standardize filename: system.kdb or {kbName}.kdb
            string fileName = kbName.Equals("system", StringComparison.OrdinalIgnoreCase) 
                ? "system.kdb" 
                : $"{kbName}.kdb";
            
            string fullPath = Path.Combine(_dataDir, fileName);
            
            var disk = new DiskManager(fullPath, _masterKey);
            var bpm = new BufferPoolManager(disk, _defaultPoolSize);
            var wal = new WalManagerV3(fullPath);
            
            var entry = (disk, bpm, wal);
            _pools[kbName] = entry;
            return entry;
        }
    }

    /// <summary>
    /// Closes and disposes of the storage components for a specific KB.
    /// </summary>
    public void CloseKb(string kbName)
    {
        lock (_lock)
        {
            if (_pools.Remove(kbName, out var managers))
            {
                managers.Bpm.Dispose();
                managers.Disk.Dispose();
                managers.Wal.Dispose();
            }
        }
    }

    /// <summary>
    /// Physically deletes a KB's storage file from disk.
    /// </summary>
    public void DeleteKbFile(string kbName)
    {
        CloseKb(kbName);
        string fileName = $"{kbName}.kdb";
        string fullPath = Path.Combine(_dataDir, fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var managers in _pools.Values)
            {
                try { managers.Bpm.Dispose(); } catch {}
                try { managers.Disk.Dispose(); } catch {}
                try { managers.Wal.Dispose(); } catch {}
            }
            _pools.Clear();
        }
    }
}
