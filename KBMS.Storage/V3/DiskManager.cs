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
    private readonly Encryption _encryption;

    // 16384 (Data) + 16 (IV) + 16 (AES Padding) = 16416 bytes on disk
    private const int DISK_BLOCK_SIZE = Page.PAGE_SIZE + 32;

    public DiskManager(string dbFilePath, string? encryptionKey = "kbms_default_system_key_v3")
    {
        _dbFilePath = dbFilePath;
        _encryption = new Encryption(encryptionKey ?? "kbms_default_system_key_v3");
        EnsureFileExists();
    }

    private void EnsureFileExists()
    {
        var directory = Path.GetDirectoryName(_dbFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_dbFilePath) || new FileInfo(_dbFilePath).Length == 0)
        {
            _dbFile = new FileStream(_dbFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            _nextPageId = 0;
            // Always allocate Page 0 as the header page immediately
            AllocatePage(); 
        }
        else
        {
            _dbFile = new FileStream(_dbFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            _nextPageId = (int)(_dbFile.Length / DISK_BLOCK_SIZE);
        }
    }

    /// <summary>
    /// Reads the requested page ID from disk into the provided Page object in RAM.
    /// </summary>
    public void ReadPage(int pageId, Page page)
    {
        if (_dbFile == null) throw new InvalidOperationException("DiskManager is not initialized.");
        
        long offset = (long)pageId * DISK_BLOCK_SIZE;
        if (offset >= _dbFile.Length)
        {
            Array.Clear(page.Data, 0, Page.PAGE_SIZE);
            return;
        }

        _dbFile.Seek(offset, SeekOrigin.Begin);
        byte[] encryptedData = new byte[DISK_BLOCK_SIZE];
        int read = _dbFile.Read(encryptedData, 0, DISK_BLOCK_SIZE);
        
        if (read > 0)
        {
            byte[] decrypted = _encryption.Decrypt(encryptedData);
            Array.Copy(decrypted, 0, page.Data, 0, Math.Min(decrypted.Length, Page.PAGE_SIZE));
            page.PageId = pageId;
        }
    }

    /// <summary>
    /// Writes the contents of a Page object from RAM to the physical disk.
    /// </summary>
    public void WritePage(int pageId, Page page)
    {
        if (_dbFile == null) throw new InvalidOperationException("DiskManager is not initialized.");

        byte[] encrypted = _encryption.Encrypt(page.Data);
        if (encrypted.Length != DISK_BLOCK_SIZE)
        {
            // Optional: log or handle size mismatch if Encryption utility changes
        }

        long offset = (long)pageId * DISK_BLOCK_SIZE;
        _dbFile.Seek(offset, SeekOrigin.Begin);
        _dbFile.Write(encrypted, 0, encrypted.Length);
        
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
