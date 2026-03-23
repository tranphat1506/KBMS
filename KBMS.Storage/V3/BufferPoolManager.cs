using System;
using System.Collections.Generic;

namespace KBMS.Storage.V3;

/// <summary>
/// The BufferPoolManager is responsible for fetching pages from the DiskManager
/// and storing them in memory (cache). It uses an LRU (Least Recently Used) 
/// replacement policy when the cache is full and a new page is requested.
/// </summary>
public class BufferPoolManager : IDisposable
{
    private readonly DiskManager _diskManager;
    private readonly int _poolSize;
    
    // The actual memory frames holding the pages
    private readonly Page[] _pages;
    
    // Maps a PageId to a FrameId (index in _pages array)
    private readonly Dictionary<int, int> _pageTable;
    
    // Tracks LRU eviction candidates (stores FrameIds)
    private readonly LinkedList<int> _lruList;
    private readonly Dictionary<int, LinkedListNode<int>> _lruNodes;
    
    // Tracks completely free (never used) frames
    private readonly Queue<int> _freeFrames;

    public BufferPoolManager(DiskManager diskManager, int poolSize = 100)
    {
        _diskManager = diskManager;
        _poolSize = poolSize;
        
        _pages = new Page[_poolSize];
        _freeFrames = new Queue<int>();
        for (int i = 0; i < _poolSize; i++)
        {
            _pages[i] = new Page();
            _freeFrames.Enqueue(i);
        }

        _pageTable = new Dictionary<int, int>();
        _lruList = new LinkedList<int>();
        _lruNodes = new Dictionary<int, LinkedListNode<int>>();
    }

    /// <summary>
    /// Fetches the requested page from the buffer pool. 
    /// If it is not in the pool, it fetches it from disk and pins it.
    /// </summary>
    public Page? FetchPage(int pageId)
    {
        // 1. If page is already in the buffer pool
        if (_pageTable.TryGetValue(pageId, out int frameId))
        {
            var page = _pages[frameId];
            page.PinCount++;
            
            // It's pinned, so remove from LRU eviction list (it's actively being used)
            if (_lruNodes.TryGetValue(frameId, out var node))
            {
                _lruList.Remove(node);
                _lruNodes.Remove(frameId);
            }
            return page;
        }

        // 2. Page is not in the pool, we must bring it in
        if (!TryGetAvailableFrame(out frameId))
        {
            return null; // All frames are pinned, cache is completely full of active pages!
        }

        // 3. Read from disk into the allocated frame
        var newPage = _pages[frameId];
        newPage.ResetMemory();
        _diskManager.ReadPage(pageId, newPage);
        
        // 4. Update metadata
        _pageTable[pageId] = frameId;
        newPage.PinCount = 1;
        newPage.IsDirty = false;

        return newPage;
    }

    /// <summary>
    /// Unpins a page, indicating the caller is done using it. 
    /// If the pin count drops to zero, it becomes a candidate for LRU eviction.
    /// </summary>
    public bool UnpinPage(int pageId, bool isDirty)
    {
        if (!_pageTable.TryGetValue(pageId, out int frameId))
            return false;

        var page = _pages[frameId];
        if (page.PinCount <= 0) return false;

        page.PinCount--;
        if (isDirty) page.IsDirty = true; // Once dirty, stays dirty until flushed

        // If no one is using it, add it back to the LRU eviction list (Least Recently Used)
        if (page.PinCount == 0 && !_lruNodes.ContainsKey(frameId))
        {
            var node = _lruList.AddLast(frameId);
            _lruNodes[frameId] = node;
        }

        return true;
    }

    /// <summary>
    /// Flushes a specific page to disk if it's dirty.
    /// </summary>
    public bool FlushPage(int pageId)
    {
        if (!_pageTable.TryGetValue(pageId, out int frameId))
            return false;

        var page = _pages[frameId];
        if (page.IsDirty)
        {
            _diskManager.WritePage(page.PageId, page);
            page.IsDirty = false;
        }
        return true;
    }

    /// <summary>
    /// Flushes all dirty pages to the disk.
    /// </summary>
    public void FlushAllPages()
    {
        foreach (var kvp in _pageTable)
        {
            FlushPage(kvp.Key);
        }
    }

    /// <summary>
    /// Creates a brand new page on the disk manager and fetches it into the pool.
    /// </summary>
    public Page? NewPage(out int pageId)
    {
        pageId = -1;
        if (!TryGetAvailableFrame(out int frameId))
            return null;

        pageId = _diskManager.AllocatePage();
        var newPage = _pages[frameId];
        newPage.ResetMemory();
        newPage.PageId = pageId;
        newPage.PinCount = 1;
        newPage.IsDirty = false;

        _pageTable[pageId] = frameId;
        return newPage;
    }

    private bool TryGetAvailableFrame(out int frameId)
    {
        // 1. Try to get a completely free frame first
        if (_freeFrames.TryDequeue(out frameId))
        {
            return true;
        }

        // 2. No free frames, try to evict via LRU (Least Recently Used is at the Front)
        if (_lruList.First != null)
        {
            var node = _lruList.First;
            frameId = node.Value;
            
            var victimPage = _pages[frameId];
            
            // If the victim was dirty, flush it to disk before overwriting
            if (victimPage.IsDirty)
            {
                _diskManager.WritePage(victimPage.PageId, victimPage);
            }

            // Remove from tracking structures
            _pageTable.Remove(victimPage.PageId);
            _lruList.Remove(node);
            _lruNodes.Remove(frameId);
            
            victimPage.ResetMemory();
            return true;
        }

        frameId = -1;
        return false;
    }

    public void Dispose()
    {
        FlushAllPages();
    }
}
