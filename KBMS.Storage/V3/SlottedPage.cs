using System;

namespace KBMS.Storage.V3;

/// <summary>
/// A SlottedPage interprets the raw bytes of a 16KB Page to store variable-length records (tuples).
/// Layout:
/// [Header (24 bytes)] 
/// [Slot Array (8 bytes per slot)] ->  ... free space ...  <- [Tuples]
/// 
/// The Slot Array grows forwards from the header.
/// The Tuples grow backwards from the end of the page.
/// This maximizes free space utilization without pre-allocating slot counts.
/// </summary>
public class SlottedPage
{
    private const int HEADER_SIZE = 24;
    private const int SLOT_SIZE = 8;
    
    private readonly Page _page;

    public SlottedPage(Page page)
    {
        _page = page;
    }

    public Page GetRawPage() => _page;

    // ================= HEADER ACCESSORS =================

    public int PageId
    {
        get => BitConverter.ToInt32(_page.Data, 0);
        set => BitConverter.GetBytes(value).CopyTo(_page.Data, 0);
    }

    public int Lsn
    {
        get => BitConverter.ToInt32(_page.Data, 4);
        set => BitConverter.GetBytes(value).CopyTo(_page.Data, 4);
    }

    public int PrevPageId
    {
        get => BitConverter.ToInt32(_page.Data, 8);
        set => BitConverter.GetBytes(value).CopyTo(_page.Data, 8);
    }

    public int NextPageId
    {
        get => BitConverter.ToInt32(_page.Data, 12);
        set => BitConverter.GetBytes(value).CopyTo(_page.Data, 12);
    }

    public int FreeSpacePointer
    {
        get => BitConverter.ToInt32(_page.Data, 16);
        set => BitConverter.GetBytes(value).CopyTo(_page.Data, 16);
    }

    public int TupleCount
    {
        get => BitConverter.ToInt32(_page.Data, 20);
        set => BitConverter.GetBytes(value).CopyTo(_page.Data, 20);
    }

    // ================= INITIALIZATION =================

    /// <summary>
    /// Initializes a fresh page with default header values.
    /// </summary>
    public void Init(int pageId, int prevPageId = -1, int nextPageId = -1)
    {
        PageId = pageId;
        Lsn = 0;
        PrevPageId = prevPageId;
        NextPageId = nextPageId;
        FreeSpacePointer = Page.PAGE_SIZE;
        TupleCount = 0;
    }

    // ================= SLOT OPERATIONS =================

    private int GetSlotOffset(int slotId)
    {
        return HEADER_SIZE + (slotId * SLOT_SIZE);
    }

    private (int offset, int length) GetSlot(int slotId)
    {
        int slotPos = GetSlotOffset(slotId);
        int offset = BitConverter.ToInt32(_page.Data, slotPos);
        int length = BitConverter.ToInt32(_page.Data, slotPos + 4);
        return (offset, length);
    }

    private void SetSlot(int slotId, int offset, int length)
    {
        int slotPos = GetSlotOffset(slotId);
        BitConverter.GetBytes(offset).CopyTo(_page.Data, slotPos);
        BitConverter.GetBytes(length).CopyTo(_page.Data, slotPos + 4);
    }

    // ================= TUPLE OPERATIONS =================

    /// <summary>
    /// Calculates remaining completely free bytes between the slot array and the tuples.
    /// </summary>
    public int GetFreeSpaceRemaining()
    {
        int slotArrayEnd = HEADER_SIZE + (TupleCount * SLOT_SIZE);
        return FreeSpacePointer - slotArrayEnd;
    }

    /// <summary>
    /// Inserts a binary tuple into the page. Returns the slot ID (Record ID).
    /// </summary>
    /// <returns>The slot ID, or -1 if there is not enough space.</returns>
    public int InsertTuple(byte[] tupleData)
    {
        // 1. Try to find an existing deleted slot
        int slotId = -1;
        for (int i = 0; i < TupleCount; i++)
        {
            var (off, len) = GetSlot(i);
            if (len == 0 && off == 0) // Slot is marked as deleted
            {
                slotId = i;
                break;
            }
        }

        // 2. If no deleted slot found, we need space for a new slot in the array
        int requiredSpace = tupleData.Length + (slotId == -1 ? SLOT_SIZE : 0);
        if (requiredSpace > GetFreeSpaceRemaining())
        {
            return -1; // Not enough space
        }

        if (slotId == -1)
        {
            slotId = TupleCount;
            TupleCount++;
        }

        // 3. Allocate free space from the back of the page
        FreeSpacePointer -= tupleData.Length;
        int offset = FreeSpacePointer;

        // 4. Write data to the allocated space
        Buffer.BlockCopy(tupleData, 0, _page.Data, offset, tupleData.Length);
        
        // 5. Update the slot pointing to this data
        SetSlot(slotId, offset, tupleData.Length);

        _page.IsDirty = true;
        return slotId;
    }

    /// <summary>
    /// Reads a binary tuple from the page given its slot ID.
    /// </summary>
    public byte[]? GetTuple(int slotId)
    {
        if (slotId < 0 || slotId >= TupleCount)
            return null;

        var (offset, length) = GetSlot(slotId);
        if (length == 0 && offset == 0) 
            return null; // Deleted record

        byte[] tuple = new byte[length];
        Buffer.BlockCopy(_page.Data, offset, tuple, 0, length);
        return tuple;
    }

    /// <summary>
    /// Marks a tuple as deleted by zeroing its slot.
    /// </summary>
    public bool DeleteTuple(int slotId)
    {
        if (slotId < 0 || slotId >= TupleCount)
            return false;

        var (offset, length) = GetSlot(slotId);
        if (length == 0 && offset == 0)
            return false; // Already deleted

        // Mark slot as deleted
        SetSlot(slotId, 0, 0);

        // NOTE: We don't compact the page immediately for performance. 
        // Fragmentation is left unresolved unless a Vacuum is implemented.
        
        _page.IsDirty = true;
        return true;
    }
}
