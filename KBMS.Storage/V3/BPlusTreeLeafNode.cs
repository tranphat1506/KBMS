using System;

namespace KBMS.Storage.V3;

/// <summary>
/// B+ Tree Leaf Node storing Guid keys (16 bytes) to RecordId values (8 bytes).
/// Total size per entry: 24 bytes.
/// Layout:
/// - Header (9 bytes) from BPlusTreeNode
/// - Next Leaf Page Id (4 bytes)
/// - Array of Keys (Guid)
/// - Array of Values (RecordId)
/// </summary>
public class BPlusTreeLeafNode : BPlusTreeNode
{
    private const int NEXT_PAGE_OFFSET = HEADER_SIZE;
    private const int KEYS_OFFSET = NEXT_PAGE_OFFSET + 4;
    private const int KEY_SIZE = 16;
    private const int VALUE_SIZE = 8;
    
    // Max capacity for 16KB page: floor((16384 - 13) / 24) = 682
    public const short MAX_CAPACITY = 680;

    public BPlusTreeLeafNode(Page page) : base(page)
    {
    }

    public int NextPageId
    {
        get => BitConverter.ToInt32(_page.Data, NEXT_PAGE_OFFSET);
        set { BitConverter.GetBytes(value).CopyTo(_page.Data, NEXT_PAGE_OFFSET); _page.IsDirty = true; }
    }

    public void Init(int parentPageId = -1)
    {
        base.Init(isLeaf: true, maxKeys: MAX_CAPACITY, parentPageId);
        NextPageId = -1;
    }

    public Guid GetKeyAt(int index)
    {
        int offset = KEYS_OFFSET + (index * KEY_SIZE);
        byte[] guidBytes = new byte[16];
        Buffer.BlockCopy(_page.Data, offset, guidBytes, 0, 16);
        return new Guid(guidBytes);
    }

    public void SetKeyAt(int index, Guid key)
    {
        int offset = KEYS_OFFSET + (index * KEY_SIZE);
        key.ToByteArray().CopyTo(_page.Data, offset);
        _page.IsDirty = true;
    }

    public RecordId GetValueAt(int index)
    {
        // Values array starts immediately after all keys
        int valuesBlockOffset = KEYS_OFFSET + (MAX_CAPACITY * KEY_SIZE);
        int offset = valuesBlockOffset + (index * VALUE_SIZE);
        
        int pageId = BitConverter.ToInt32(_page.Data, offset);
        int slotId = BitConverter.ToInt32(_page.Data, offset + 4);
        return new RecordId(pageId, slotId);
    }

    public void SetValueAt(int index, RecordId value)
    {
        int valuesBlockOffset = KEYS_OFFSET + (MAX_CAPACITY * KEY_SIZE);
        int offset = valuesBlockOffset + (index * VALUE_SIZE);
        
        BitConverter.GetBytes(value.PageId).CopyTo(_page.Data, offset);
        BitConverter.GetBytes(value.SlotId).CopyTo(_page.Data, offset + 4);
        _page.IsDirty = true;
    }

    public void InsertAt(int index, Guid key, RecordId value)
    {
        // Shift existing elements right
        for (int i = KeyCount; i > index; i--)
        {
            SetKeyAt(i, GetKeyAt(i - 1));
            SetValueAt(i, GetValueAt(i - 1));
        }

        SetKeyAt(index, key);
        SetValueAt(index, value);
        KeyCount++;
    }
}
