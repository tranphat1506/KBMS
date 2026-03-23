using System;

namespace KBMS.Storage.V3;

/// <summary>
/// Represents a fixed-size block of memory synced with the OS file system.
/// In KBMS V3, pages are 16KB in size to balance I/O overhead and capacity.
/// </summary>
public class Page
{
    public const int PAGE_SIZE = 16384; // 16 KB

    // Unique identifier for this page within the file
    public int PageId { get; set; }

    // The actual raw bytes of the page
    public byte[] Data { get; } = new byte[PAGE_SIZE];

    // Tracks if the page has been modified since it was loaded from disk
    public bool IsDirty { get; set; } = false;

    // Tracks how many threads/components are currently using this page
    public int PinCount { get; set; } = 0;

    /// <summary>
    /// Clears the page data to prepare it for reuse in the object pool.
    /// </summary>
    public void ResetMemory()
    {
        Array.Clear(Data, 0, PAGE_SIZE);
        IsDirty = false;
        PinCount = 0;
        PageId = -1;
    }
}
