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
    private readonly BufferPoolManager _bpm;
    private readonly DiskManager _diskManager;

    private readonly List<int> _pageIds = new();
    private readonly object _lock = new();

    private const string CATALOG_KEY = "catalog:kbs";

    public KbCatalog(BufferPoolManager bpm, DiskManager diskManager)
    {
        _bpm = bpm;
        _diskManager = diskManager;
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

        var data = SerializeKb(kb);

        lock (_lock)
        {
            var pageId = GetOrAllocatePage(data.Length);
            var page = _bpm.FetchPage(pageId);
            if (page == null) throw new Exception("Could not fetch page for KB catalog.");

            var sp = new SlottedPage(page);
            if (sp.TupleCount == 0 && sp.FreeSpacePointer == 0) sp.Init(page.PageId);
            var slotId = sp.InsertTuple(data);

            if (slotId < 0)
            {
                _bpm.UnpinPage(page.PageId, false);
                var newPageId = _diskManager.AllocatePage();
                _pageIds.Add(newPageId);

                page = _bpm.FetchPage(newPageId);
                if (page == null) throw new Exception("Could not fetch new page for KB catalog.");
                sp = new SlottedPage(page);
                sp.Init(newPageId);
                sp.InsertTuple(data);
            }

            _bpm.UnpinPage(page.PageId, true);
        }

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

        foreach (var pageId in pageSnapshot)
        {
            var page = _bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null || raw.Length == 0) continue;

                var kb = DeserializeKb(raw);
                if (kb != null) results.Add(kb);
            }

            _bpm.UnpinPage(page.PageId, false);
        }

        return results;
    }

    // ===================== UPDATE =====================

    public bool SaveKbMetadata(KnowledgeBase kb)
    {
        if (!DropKb(kb.Name)) return false;

        var data = SerializeKb(kb);
        lock (_lock)
        {
            var pageId = GetOrAllocatePage(data.Length);
            var page = _bpm.FetchPage(pageId);
            if (page == null) return false;

            var sp = new SlottedPage(page);
            if (sp.TupleCount == 0 && sp.FreeSpacePointer == 0) sp.Init(page.PageId);
            var slotId = sp.InsertTuple(data);

            if (slotId < 0)
            {
                _bpm.UnpinPage(page.PageId, false);
                var newPageId = _diskManager.AllocatePage();
                _pageIds.Add(newPageId);

                page = _bpm.FetchPage(newPageId);
                if (page == null) return false;
                sp = new SlottedPage(page);
                sp.Init(newPageId);
                sp.InsertTuple(data);
            }

            _bpm.UnpinPage(page.PageId, true);
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

        foreach (var pageId in pageSnapshot)
        {
            var page = _bpm.FetchPage(pageId);
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
                    _bpm.UnpinPage(page.PageId, true);
                    return true;
                }
            }

            _bpm.UnpinPage(page.PageId, false);
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

    // ===================== HELPERS =====================

    private int GetOrAllocatePage(int dataSize)
    {
        if (_pageIds.Count == 0)
        {
            var id = _diskManager.AllocatePage();
            _pageIds.Add(id);
            return id;
        }

        return _pageIds[^1];
    }

    public bool KbExists(string name) => LoadKb(name) != null;
}
