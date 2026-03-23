using System;
using System.IO;

namespace KBMS.Storage.V3;

/// <summary>
/// DiskManager handles the physical reading and writing of Pages to the OS file system.
/// It uses FileStream.Seek to ensure $O(1)$ random access to any 16KB block, 
/// bypassing the need to load the entire database into RAM.
/// </summary>
public class DiskManager : IDisposable
{
    private readonly string _dbFilePath;
    private FileStream? _dbFile;
    private int _nextPageId = 0;

    public DiskManager(string dbFilePath)
    {
        _dbFilePath = dbFilePath;
        EnsureFileExists();
    }

    private void EnsureFileExists()
    {
        var directory = Path.GetDirectoryName(_dbFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_dbFilePath))
        {
            _dbFile = new FileStream(_dbFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            _nextPageId = 0;
        }
        else
        {
            _dbFile = new FileStream(_dbFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            // Calculate next page ID based on file size
            _nextPageId = (int)(_dbFile.Length / Page.PAGE_SIZE);
        }
    }

    /// <summary>
    /// Reads the requested page ID from disk into the provided Page object in RAM.
    /// </summary>
    public void ReadPage(int pageId, Page page)
    {
        if (_dbFile == null) throw new InvalidOperationException("DiskManager is not initialized.");
        
        long offset = (long)pageId * Page.PAGE_SIZE;
        if (offset >= _dbFile.Length)
        {
            Array.Clear(page.Data, 0, Page.PAGE_SIZE); // Padding for out-of-bounds reads
            return;
        }

        _dbFile.Seek(offset, SeekOrigin.Begin);
        
        // ReadExactly ensures we pull exactly 16KB
        _dbFile.ReadExactly(page.Data, 0, Page.PAGE_SIZE);
        page.PageId = pageId;
    }

    /// <summary>
    /// Writes the contents of a Page object from RAM to the physical disk.
    /// </summary>
    public void WritePage(int pageId, Page page)
    {
        if (_dbFile == null) throw new InvalidOperationException("DiskManager is not initialized.");

        long offset = (long)pageId * Page.PAGE_SIZE;
        _dbFile.Seek(offset, SeekOrigin.Begin);
        _dbFile.Write(page.Data, 0, Page.PAGE_SIZE);
        
        // Ensure OS flushes to physical media
        _dbFile.Flush(flushToDisk: true); 
    }

    /// <summary>
    /// Allocates a new empty page at the end of the file.
    /// </summary>
    /// <returns>The ID of the newly allocated page.</returns>
    public int AllocatePage()
    {
        int pageId = _nextPageId++;
        
        // Zero-fill the new page on disk immediately
        var emptyPage = new Page { PageId = pageId };
        WritePage(pageId, emptyPage);
        
        return pageId;
    }
    
    /// <summary>
    /// Gets the total number of allocated pages in the file.
    /// </summary>
    public int GetTotalPages() => _nextPageId;

    public void Dispose()
    {
        _dbFile?.Flush();
        _dbFile?.Dispose();
    }
}
