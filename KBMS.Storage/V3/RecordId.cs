namespace KBMS.Storage.V3;

/// <summary>
/// A global identifier for a physical record inside the database file.
/// It consists of the physical Page ID and the Slot ID within that page.
/// </summary>
public struct RecordId
{
    public int PageId { get; set; }
    public int SlotId { get; set; }

    public RecordId(int pageId, int slotId)
    {
        PageId = pageId;
        SlotId = slotId;
    }

    public override string ToString()
    {
        return $"[Page: {PageId}, Slot: {SlotId}]";
    }

    public bool Equals(RecordId other)
    {
        return PageId == other.PageId && SlotId == other.SlotId;
    }
}
