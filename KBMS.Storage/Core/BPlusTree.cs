using System;

namespace KBMS.Storage.Core;

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

    public RecordId? Search(Guid key)
    {
        return SearchRecursive(_rootPageId, key);
    }

    private RecordId? SearchRecursive(int pageId, Guid key)
    {
        var page = _bpm.FetchPage(pageId);
        if (page == null) return null;

        bool isLeaf = page.Data[0] == 1;
        RecordId? result = null;

        if (isLeaf)
        {
            var leaf = new BPlusTreeLeafNode(page);
            for (int i = 0; i < leaf.KeyCount; i++)
            {
                if (leaf.GetKeyAt(i).CompareTo(key) == 0)
                {
                    result = leaf.GetValueAt(i);
                    break;
                }
            }
        }
        else
        {
            var internalNode = new BPlusTreeInternalNode(page);
            int childIndex = 0;
            while (childIndex < internalNode.KeyCount && key.CompareTo(internalNode.GetKeyAt(childIndex)) >= 0)
            {
                childIndex++;
            }
            int childPageId = internalNode.GetValueAt(childIndex);
            
            _bpm.UnpinPage(pageId, false);
            return SearchRecursive(childPageId, key);
        }

        _bpm.UnpinPage(pageId, false);
        return result;
    }

    public void Insert(Guid key, RecordId value)
    {
        InsertRecursive(_rootPageId, key, value);
    }

    private (Guid? splitKey, int? newPageId) InsertRecursive(int pageId, Guid key, RecordId value)
    {
        var page = _bpm.FetchPage(pageId);
        if (page == null) throw new Exception("Failed to fetch page.");

        bool isLeaf = page.Data[0] == 1;
        (Guid? splitKey, int? newPageId) result = (null, null);

        if (isLeaf)
        {
            var leaf = new BPlusTreeLeafNode(page);
            int insertIndex = 0;
            while (insertIndex < leaf.KeyCount && key.CompareTo(leaf.GetKeyAt(insertIndex)) > 0)
            {
                insertIndex++;
            }

            if (insertIndex < leaf.KeyCount && key.CompareTo(leaf.GetKeyAt(insertIndex)) == 0)
            {
                leaf.SetValueAt(insertIndex, value);
            }
            else
            {
                if (leaf.KeyCount < leaf.MaxKeys)
                {
                    leaf.InsertAt(insertIndex, key, value);
                }
                else
                {
                    var newPage = _bpm.NewPage(out int newLeafPageId);
                    if (newPage == null) throw new Exception("Failed to allocate new leaf page.");
                    var newLeaf = new BPlusTreeLeafNode(newPage);
                    newLeaf.Init(leaf.ParentPageId);
                    
                    int mid = leaf.MaxKeys / 2;
                    int newLeafCount = 0;
                    
                    for (int i = mid; i < leaf.KeyCount; i++)
                    {
                        newLeaf.InsertAt(newLeafCount++, leaf.GetKeyAt(i), leaf.GetValueAt(i));
                    }
                    leaf.KeyCount = (short)mid;
                    
                    if (insertIndex < mid)
                    {
                        leaf.InsertAt(insertIndex, key, value);
                    }
                    else
                    {
                        newLeaf.InsertAt(insertIndex - mid, key, value);
                    }
                    
                    newLeaf.NextPageId = leaf.NextPageId;
                    leaf.NextPageId = newLeafPageId;

                    result = (newLeaf.GetKeyAt(0), newLeafPageId);
                    _bpm.UnpinPage(newLeafPageId, true);
                }
            }
        }
        else
        {
            var internalNode = new BPlusTreeInternalNode(page);
            int childIndex = 0;
            while (childIndex < internalNode.KeyCount && key.CompareTo(internalNode.GetKeyAt(childIndex)) >= 0)
            {
                childIndex++;
            }
            
            int childPageId = internalNode.GetValueAt(childIndex);
            var childSplit = InsertRecursive(childPageId, key, value);
            
            if (childSplit.splitKey.HasValue && childSplit.newPageId.HasValue)
            {
                if (internalNode.KeyCount < internalNode.MaxKeys)
                {
                    internalNode.InsertAt(childIndex, childSplit.splitKey.Value, childSplit.newPageId.Value);
                }
                else
                {
                    var newPage = _bpm.NewPage(out int newInternalPageId);
                    if (newPage == null) throw new Exception("Failed to allocate new internal page.");
                    var newInternal = new BPlusTreeInternalNode(newPage);
                    newInternal.Init(internalNode.ParentPageId);
                    
                    int mid = internalNode.MaxKeys / 2;
                    var pushUpKey = internalNode.GetKeyAt(mid);
                    
                    int newIntCount = 0;
                    for (int i = mid + 1; i < internalNode.KeyCount; i++)
                    {
                        newInternal.SetKeyAt(newIntCount, internalNode.GetKeyAt(i));
                        newInternal.SetValueAt(newIntCount, internalNode.GetValueAt(i));
                        newIntCount++;
                    }
                    newInternal.SetValueAt(newIntCount, internalNode.GetValueAt(internalNode.KeyCount));
                    newInternal.KeyCount = (short)newIntCount;
                    internalNode.KeyCount = (short)mid;

                    if (childIndex <= mid)
                    {
                        internalNode.InsertAt(childIndex, childSplit.splitKey.Value, childSplit.newPageId.Value);
                    }
                    else
                    {
                        newInternal.InsertAt(childIndex - mid - 1, childSplit.splitKey.Value, childSplit.newPageId.Value);
                    }

                    result = (pushUpKey, newInternalPageId);
                    _bpm.UnpinPage(newInternalPageId, true);
                }
            }
        }

        _bpm.UnpinPage(pageId, true);
        
        if (pageId == _rootPageId && result.splitKey.HasValue && result.newPageId.HasValue)
        {
            var newRootPage = _bpm.NewPage(out int newRootPageId);
            if (newRootPage == null) throw new Exception("Failed to allocate new root page.");
            var newRoot = new BPlusTreeInternalNode(newRootPage);
            newRoot.Init();
            
            newRoot.SetValueAt(0, _rootPageId);
            newRoot.InsertAt(0, result.splitKey.Value, result.newPageId.Value);
            
            _rootPageId = newRootPageId;
            _bpm.UnpinPage(newRootPageId, true);
            result = (null, null);
        }

        return result;
    }
}
