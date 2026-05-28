using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace KBMS.Storage.Core;

/// <summary>
/// Manages B+ Tree indexes for concepts within a Knowledge Base.
/// </summary>
public class IndexManager
{
    private readonly StoragePool _pool;
    
    // In-memory mapping of CatalogKey (kbName:conceptName:attributeName) to BPlusTree root page IDs
    private readonly ConcurrentDictionary<string, int> _indexRoots = new();
    
    // Cache for loaded B+ Trees
    private readonly ConcurrentDictionary<string, BPlusTree> _trees = new();

    public IndexManager(StoragePool pool)
    {
        _pool = pool;
    }

    /// <summary>
    /// Gets or creates a B+ Tree index for a specific concept attribute.
    /// </summary>
    public BPlusTree GetOrCreateIndex(string kbName, string conceptName, string attributeName)
    {
        var indexKey = $"{kbName}:{conceptName}:{attributeName}";
        
        if (_trees.TryGetValue(indexKey, out var tree))
        {
            return tree;
        }

        var bpm = _pool.GetManagers(kbName).Bpm;

        if (_indexRoots.TryGetValue(indexKey, out var rootPageId))
        {
            tree = new BPlusTree(bpm, rootPageId);
        }
        else
        {
            tree = new BPlusTree(bpm);
            _indexRoots[indexKey] = tree.GetRootPageId();
        }

        _trees[indexKey] = tree;
        return tree;
    }

    /// <summary>
    /// Inserts a new entry into the index.
    /// </summary>
    public void InsertIndexEntry(string kbName, string conceptName, string attributeName, string valueKey, RecordId value)
    {
        var tree = GetOrCreateIndex(kbName, conceptName, attributeName);
        var guidKey = HashStringToGuid(valueKey);
        tree.Insert(guidKey, value);
    }

    /// <summary>
    /// Searches the index for a specific entry.
    /// </summary>
    public RecordId? SearchIndex(string kbName, string conceptName, string attributeName, string valueKey)
    {
        var tree = GetOrCreateIndex(kbName, conceptName, attributeName);
        var guidKey = HashStringToGuid(valueKey);
        return tree.Search(guidKey);
    }

    private Guid HashStringToGuid(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
    }

    public void DropIndex(string kbName)
    {
        var prefix = kbName + ":";
        var keysToRemove = new List<string>();
        foreach (var key in _indexRoots.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove)
        {
            _indexRoots.TryRemove(key, out _);
            _trees.TryRemove(key, out _);
        }
    }
}
