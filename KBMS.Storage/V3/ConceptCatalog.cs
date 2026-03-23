using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using KBMS.Models;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Storage.V3;

/// <summary>
/// V3 Concept Catalog: stores Concept schema records in binary SlottedPages
/// managed by the BufferPoolManager instead of JSON .kmf files.
///
/// Each Concept is serialized to a JSON-encoded byte array and stored as a single Tuple.
/// The catalog key is "catalog:concepts:{kbName}" → list of pageIds.
///
/// This replaces Engine.cs methods: CreateConcept, LoadConcept, ListConcepts, DropConcept.
/// </summary>
public class ConceptCatalog
{
    private readonly BufferPoolManager _bpm;
    private readonly DiskManager _diskManager;

    // In-memory page catalog: "catalog:concepts:{kbName}" -> pageIds
    private readonly Dictionary<string, List<int>> _pageMap = new();
    private readonly object _lock = new();

    public ConceptCatalog(BufferPoolManager bpm, DiskManager diskManager)
    {
        _bpm = bpm;
        _diskManager = diskManager;
    }

    // ===================== CREATE =====================

    public bool CreateConcept(string kbName, Concept concept)
    {
        // Reject duplicates
        if (LoadConcept(kbName, concept.Name) != null)
            return false;

        var data = SerializeConcept(concept);
        var key = GetKey(kbName);

        lock (_lock)
        {
            var pageId = GetOrAllocatePage(key, data.Length);
            var page = _bpm.FetchPage(pageId);
            if (page == null) return false;

            var sp = new SlottedPage(page);
            if (sp.TupleCount == 0 && sp.FreeSpacePointer == 0) sp.Init(pageId);
            var slotId = sp.InsertTuple(data);

            if (slotId < 0)
            {
                _bpm.UnpinPage(page.PageId, false);
                var newPageId = _diskManager.AllocatePage();
                _pageMap[key].Add(newPageId);

                page = _bpm.FetchPage(newPageId);
                if (page == null) return false;
                sp = new SlottedPage(page);
                sp.Init(newPageId);
                slotId = sp.InsertTuple(data);
            }

            _bpm.UnpinPage(page.PageId, true);
            return slotId >= 0;
        }
    }

    // ===================== READ =====================

    public Concept? LoadConcept(string kbName, string conceptName)
    {
        return ListConcepts(kbName)
            .FirstOrDefault(c => c.Name.Equals(conceptName, StringComparison.OrdinalIgnoreCase));
    }

    public List<Concept> ListConcepts(string kbName)
    {
        var results = new List<Concept>();
        var key = GetKey(kbName);

        List<int> pageIds;
        lock (_lock)
        {
            if (!_pageMap.TryGetValue(key, out var ids)) return results;
            pageIds = new List<int>(ids);
        }

        foreach (var pageId in pageIds)
        {
            var page = _bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null || raw.Length == 0) continue;

                var concept = DeserializeConcept(raw);
                if (concept != null) results.Add(concept);
            }

            _bpm.UnpinPage(page.PageId, false);
        }

        return results;
    }

    // ===================== UPDATE =====================

    public bool UpdateConcept(string kbName, Concept updatedConcept)
    {
        if (!DropConcept(kbName, updatedConcept.Name))
            return false;
        return CreateConcept(kbName, updatedConcept);
    }

    // ===================== DELETE =====================

    public bool DropConcept(string kbName, string conceptName)
    {
        var key = GetKey(kbName);
        List<int> pageIds;

        lock (_lock)
        {
            if (!_pageMap.TryGetValue(key, out var ids)) return false;
            pageIds = new List<int>(ids);
        }

        foreach (var pageId in pageIds)
        {
            var page = _bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null) continue;

                var concept = DeserializeConcept(raw);
                if (concept != null && concept.Name.Equals(conceptName, StringComparison.OrdinalIgnoreCase))
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

    // ===================== CATALOG MANAGEMENT =====================

    /// <summary>
    /// Drops all concept records for a given KB (used when dropping a KB).
    /// </summary>
    public void DropAllConcepts(string kbName)
    {
        lock (_lock)
        {
            _pageMap.Remove(GetKey(kbName));
        }
    }

    // ===================== SERIALIZATION =====================

    private byte[] SerializeConcept(Concept concept)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(concept));
    }

    private Concept? DeserializeConcept(byte[] data)
    {
        try { return JsonSerializer.Deserialize<Concept>(Encoding.UTF8.GetString(data)); }
        catch { return null; }
    }

    // ===================== HELPERS =====================

    private string GetKey(string kbName) => $"catalog:concepts:{kbName}";

    private int GetOrAllocatePage(string key, int dataSize)
    {
        if (!_pageMap.ContainsKey(key) || _pageMap[key].Count == 0)
        {
            var newId = _diskManager.AllocatePage();
            _pageMap[key] = new List<int> { newId };
            return newId;
        }
        return _pageMap[key][^1];
    }

    // Allow external systems to expose the page map count for diagnostics
    public int TotalCatalogPages { get { lock (_lock) { return _pageMap.Values.Sum(v => v.Count); } } }
}
