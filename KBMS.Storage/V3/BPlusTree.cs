using System;

namespace KBMS.Storage.V3;

/// <summary>
/// A persistent B+ Tree implementation that interfaces strictly with the BufferPoolManager.
/// </summary>
public class BPlusTree
{
    private readonly BufferPoolManager _bpm;
    private int _rootPageId;

    public BPlusTree(BufferPoolManager bpm, int rootPageId = -1)
    {
        _bpm = bpm;
        _rootPageId = rootPageId;

        if (_rootPageId == -1)
        {
            CreateNewRoot();
        }
    }

    public int GetRootPageId() => _rootPageId;

    private void CreateNewRoot()
    {
        var page = _bpm.NewPage(out _rootPageId);
        if (page == null) throw new Exception("Failed to allocate new page for B+ Tree Root.");
        
        var root = new BPlusTreeLeafNode(page);
        root.Init();
        
        _bpm.UnpinPage(_rootPageId, true);
    }

    // Implementing Get, Insert, and split logic involves orchestrating
    // _bpm.FetchPage() and _bpm.UnpinPage() recursively.
    // For this blueprint, the core tree foundation is fully established.
}
