using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using KBMS.Models;

namespace KBMS.Storage.V3;

/// <summary>
/// V3 KB Catalog: stores KnowledgeBase metadata records in binary SlottedPages
/// managed by the BufferPoolManager instead of per-KB directory + .kmf files.
///
/// Replaces Engine.cs methods: CreateKb, LoadKb, ListKbs, DropKb, SaveKbMetadata.
///
/// Layout: all KBs share a single set of "catalog:kbs" pages in the global .kdb file.
/// </summary>
public class KbCatalog
{
    private readonly StoragePool _storagePool;
    private readonly List<int> _pageIds = new();
    private readonly object _lock = new();

    private const string CATALOG_KEY = "catalog:kbs";

    public KbCatalog(StoragePool storagePool)
    {
        _storagePool = storagePool;
        LoadPageIds();
    }

    // ===================== CREATE =====================

    public KnowledgeBase CreateKb(string name, Guid ownerId, string description = "")
    {
        if (LoadKb(name) != null)
            throw new InvalidOperationException($"Knowledge base '{name}' already exists.");

        var kb = new KnowledgeBase
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.Now,
            OwnerId = ownerId,
            Description = description,
            ObjectCount = 0,
            RuleCount = 0
        };

        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;
        var diskManager = managers.Disk;

        var data = SerializeKb(kb);

        lock (_lock)
        {
            var pageId = GetOrAllocatePage();
            var page = bpm.FetchPage(pageId);
            if (page == null) throw new Exception("Could not fetch page for KB catalog.");

            var sp = new SlottedPage(page);
            if (sp.TupleCount == 0 && sp.FreeSpacePointer == 0) sp.Init(page.PageId);
            var slotId = sp.InsertTuple(data);

            // Ensure the page ID is recorded in the header if it's the first time
            if (!_pageIds.Contains(page.PageId))
            {
                _pageIds.Add(page.PageId);
                SavePageIds();
            }

            if (slotId < 0)
            {
                bpm.UnpinPage(page.PageId, false);
                var newPageId = diskManager.AllocatePage();
                _pageIds.Add(newPageId);
                SavePageIds(); // Persist the new page ID

                page = bpm.FetchPage(newPageId);
                if (page == null) throw new Exception("Could not fetch new page for KB catalog.");
                sp = new SlottedPage(page);
                sp.Init(newPageId);
                sp.InsertTuple(data);
            }

            bpm.UnpinPage(page.PageId, true);
        }

        // Initialize the KB's own storage file
        _storagePool.GetManagers(name);

        return kb;
    }

    // ===================== READ =====================

    public KnowledgeBase? LoadKb(string name)
    {
        return ListKbs().FirstOrDefault(kb => kb.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public List<KnowledgeBase> ListKbs()
    {
        var results = new List<KnowledgeBase>();

        List<int> pageSnapshot;
        lock (_lock) { pageSnapshot = new List<int>(_pageIds); }

        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;

        foreach (var pageId in pageSnapshot)
        {
            var page = bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null || raw.Length == 0) continue;

                var kb = DeserializeKb(raw);
                if (kb != null) results.Add(kb);
            }

            bpm.UnpinPage(page.PageId, false);
        }

        return results;
    }

    // ===================== UPDATE =====================

    public bool SaveKbMetadata(KnowledgeBase kb)
    {
        if (!DropKb(kb.Name)) return false;

        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;
        var diskManager = managers.Disk;

        var data = SerializeKb(kb);
        lock (_lock)
        {
            var pageId = GetOrAllocatePage();
            var page = bpm.FetchPage(pageId);
            if (page == null) return false;

            var sp = new SlottedPage(page);
            if (sp.TupleCount == 0 && sp.FreeSpacePointer == 0) sp.Init(page.PageId);
            var slotId = sp.InsertTuple(data);

            if (slotId < 0)
            {
                bpm.UnpinPage(page.PageId, false);
                var newPageId = diskManager.AllocatePage();
                _pageIds.Add(newPageId);
                SavePageIds(); // Persist

                page = bpm.FetchPage(newPageId);
                if (page == null) return false;
                sp = new SlottedPage(page);
                sp.Init(newPageId);
                sp.InsertTuple(data);
            }

            bpm.UnpinPage(page.PageId, true);
        }
        return true;
    }

    // ===================== DELETE =====================

    public bool DropKb(string name)
    {
        if (name.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        List<int> pageSnapshot;
        lock (_lock) { pageSnapshot = new List<int>(_pageIds); }

        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;

        foreach (var pageId in pageSnapshot)
        {
            var page = bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null) continue;

                var kb = DeserializeKb(raw);
                if (kb != null && kb.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    sp.DeleteTuple(i);
                    bpm.UnpinPage(page.PageId, true);
                    return true;
                }
            }

            bpm.UnpinPage(page.PageId, false);
        }

        return false;
    }

    // ===================== SERIALIZATION =====================

    private byte[] SerializeKb(KnowledgeBase kb)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(kb));

    private KnowledgeBase? DeserializeKb(byte[] data)
    {
        try { return JsonSerializer.Deserialize<KnowledgeBase>(Encoding.UTF8.GetString(data)); }
        catch { return null; }
    }

    // ===================== PERSISTENCE =====================

    private void LoadPageIds()
    {
        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;
        var page = bpm.FetchPage(0); // Page 0 is reserved for system header
        if (page == null) return;

        try
        {
            // Simple format: [Count (4 bytes)][PageId1 (4 bytes)][PageId2 (4 bytes)]...
            int count = BitConverter.ToInt32(page.Data, 0);
            if (count > 0 && count < 1000) // Sanity check
            {
                for (int i = 0; i < count; i++)
                {
                    int id = BitConverter.ToInt32(page.Data, 4 + (i * 4));
                    if (id > 0) _pageIds.Add(id);
                }
            }
        }
        catch { }
        finally { bpm.UnpinPage(0, false); }
    }

    private void SavePageIds()
    {
        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;
        var wal = managers.Wal;
        var page = bpm.FetchPage(0);
        if (page == null) return;

        try
        {
            var txnId = wal.Begin();
            byte[] before = (byte[])page.Data.Clone();

            BitConverter.GetBytes(_pageIds.Count).CopyTo(page.Data, 0);
            for (int i = 0; i < _pageIds.Count; i++)
            {
                BitConverter.GetBytes(_pageIds[i]).CopyTo(page.Data, 4 + (i * 4));
            }

            wal.LogWrite(txnId, 0, before, page.Data);
            bpm.UnpinPage(0, true);
            bpm.FlushPage(0);
            wal.Commit(txnId);
        }
        catch { bpm.UnpinPage(0, false); }
    }

    // ===================== HELPERS =====================

    private int GetOrAllocatePage()
    {
        var managers = _storagePool.GetManagers("system");
        var diskManager = managers.Disk;

        if (_pageIds.Count == 0)
        {
            var id = diskManager.AllocatePage();
            _pageIds.Add(id);
            SavePageIds(); // Persist the first page ID
            return id;
        }

        return _pageIds[^1];
    }

    public bool KbExists(string name) => LoadKb(name) != null;
}
