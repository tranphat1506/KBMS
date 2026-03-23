using System;

namespace KBMS.Storage.V3;

/// <summary>
/// B+ Tree Internal Node storing Guid keys (16 bytes) and Child Page Ids (4 bytes).
/// An internal node with M keys has M+1 children.
/// Layout:
/// - Header (9 bytes) from BPlusTreeNode
/// - Array of Keys (Guid)
/// - Array of Child Page Ids (int)
/// </summary>
public class BPlusTreeInternalNode : BPlusTreeNode
{
    private const int KEYS_OFFSET = HEADER_SIZE;
    private const int KEY_SIZE = 16;
    private const int VALUE_SIZE = 4;
    
    // Max capacity for 16KB page: floor((16384 - 9 - 4) / 20) = 818
    public const short MAX_CAPACITY = 810;

    public BPlusTreeInternalNode(Page page) : base(page)
    {
    }

    public void Init(int parentPageId = -1)
    {
        base.Init(isLeaf: false, maxKeys: MAX_CAPACITY, parentPageId);
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

    // Notice: M keys mean M+1 values (children)
    public int GetValueAt(int index)
    {
        int valuesBlockOffset = KEYS_OFFSET + (MAX_CAPACITY * KEY_SIZE);
        int offset = valuesBlockOffset + (index * VALUE_SIZE);
        return BitConverter.ToInt32(_page.Data, offset);
    }

    public void SetValueAt(int index, int value)
    {
        int valuesBlockOffset = KEYS_OFFSET + (MAX_CAPACITY * KEY_SIZE);
        int offset = valuesBlockOffset + (index * VALUE_SIZE);
        BitConverter.GetBytes(value).CopyTo(_page.Data, offset);
        _page.IsDirty = true;
    }

    public void InsertAt(int index, Guid key, int rightChildPageId)
    {
        // Shift keys right
        for (int i = KeyCount; i > index; i--)
        {
            SetKeyAt(i, GetKeyAt(i - 1));
        }

        // Shift values (children) right. Values array has KeyCount + 1 valid elements.
        for (int i = KeyCount + 1; i > index + 1; i--)
        {
            SetValueAt(i, GetValueAt(i - 1));
        }

        SetKeyAt(index, key);
        SetValueAt(index + 1, rightChildPageId);
        KeyCount++;
    }
}
