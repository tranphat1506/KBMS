using System;

namespace KBMS.Storage.V3;

/// <summary>
/// Base class for B+ Tree Nodes mapped to a 16KB Page.
/// Layout Header (9 bytes):
/// - Node Type (1 byte: 0 = Internal, 1 = Leaf)
/// - Key Count (2 bytes)
/// - Max Keys (2 bytes)
/// - Parent Page Id (4 bytes)
/// </summary>
public abstract class BPlusTreeNode
{
    protected const int HEADER_SIZE = 9;
    protected readonly Page _page;

    protected BPlusTreeNode(Page page)
    {
        _page = page;
    }

    public Page GetRawPage() => _page;

    public bool IsLeaf
    {
        get => _page.Data[0] == 1;
        set { _page.Data[0] = (byte)(value ? 1 : 0); _page.IsDirty = true; }
    }

    public short KeyCount
    {
        get => BitConverter.ToInt16(_page.Data, 1);
        set { BitConverter.GetBytes(value).CopyTo(_page.Data, 1); _page.IsDirty = true; }
    }

    public short MaxKeys
    {
        get => BitConverter.ToInt16(_page.Data, 3);
        set { BitConverter.GetBytes(value).CopyTo(_page.Data, 3); _page.IsDirty = true; }
    }

    public int ParentPageId
    {
        get => BitConverter.ToInt32(_page.Data, 5);
        set { BitConverter.GetBytes(value).CopyTo(_page.Data, 5); _page.IsDirty = true; }
    }

    public void Init(bool isLeaf, short maxKeys, int parentPageId = -1)
    {
        IsLeaf = isLeaf;
        KeyCount = 0;
        MaxKeys = maxKeys;
        ParentPageId = parentPageId;
    }
}
