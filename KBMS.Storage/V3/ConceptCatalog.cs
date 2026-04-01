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
    private readonly StoragePool _storagePool;
    private readonly object _lock = new();

    public ConceptCatalog(StoragePool storagePool)
    {
        _storagePool = storagePool;
    }

    // ===================== CREATE =====================

    public bool CreateConcept(string kbName, Concept concept)
    {
        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;
        var diskManager = managers.Disk;

        var data = SerializeConcept(concept);
        var key = "catalog:concepts";

        lock (_lock)
        {
            if (LoadConcept(kbName, concept.Name) != null)
                return false;

            var pageId = GetOrAllocatePage(kbName, key, data.Length);

            var page = bpm.FetchPage(pageId);
            if (page == null) return false;

            var sp = new SlottedPage(page);
            if (sp.TupleCount == 0 && sp.FreeSpacePointer == 0) sp.Init(pageId);
            var slotId = sp.InsertTuple(data);

            if (slotId < 0)
            {
                bpm.UnpinPage(page.PageId, false);
                var newPageId = diskManager.AllocatePage();
                
                // Add to internal tracking
                _pageMap.TryAdd(kbName, new List<int>());
                _pageMap[kbName].Add(newPageId);

                page = bpm.FetchPage(newPageId);
                if (page == null) return false;
                sp = new SlottedPage(page);
                sp.Init(newPageId);
                slotId = sp.InsertTuple(data);
            }

            bpm.UnpinPage(page.PageId, true);
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
        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;

        List<int> pageIds;
        lock (_lock)
        {
            if (!_pageMap.ContainsKey(kbName)) LoadPageIds(kbName);
            if (!_pageMap.TryGetValue(kbName, out var ids)) return results;
            pageIds = new List<int>(ids);
        }

        foreach (var pageId in pageIds)
        {
            var page = bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null || raw.Length == 0) continue;

                var concept = DeserializeConcept(raw);
                if (concept != null) results.Add(concept);
            }

            bpm.UnpinPage(page.PageId, false);
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
        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;
        List<int> pageIds;

        lock (_lock)
        {
            if (!_pageMap.TryGetValue(kbName, out var ids)) return false;
            pageIds = new List<int>(ids);
        }

        foreach (var pageId in pageIds)
        {
            var page = bpm.FetchPage(pageId);
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
                    bpm.UnpinPage(page.PageId, true);
                    return true;
                }
            }

            bpm.UnpinPage(page.PageId, false);
        }

        return false;
    }

    // ===================== CATALOG MANAGEMENT =====================

    public void DropAllConcepts(string kbName)
    {
        lock (_lock)
        {
            _pageMap.Remove(kbName);
        }
    }

    // ===================== SERIALIZATION =====================

    private byte[] SerializeConcept(Concept concept) => ModelBinaryUtility.SerializeConcept(concept);

    private Concept? DeserializeConcept(byte[] data) => ModelBinaryUtility.DeserializeConcept(data);

    // ===================== HELPERS =====================

    private int GetOrAllocatePage(string kbName, string key, int dataSize)
    {
        var managers = _storagePool.GetManagers(kbName);
        var diskManager = managers.Disk;

        lock (_lock)
        {
            if (!_pageMap.ContainsKey(kbName)) LoadPageIds(kbName);
            if (!_pageMap.ContainsKey(kbName) || _pageMap[kbName].Count == 0)
            {
                var newId = diskManager.AllocatePage();
                _pageMap[kbName] = new List<int> { newId };
                SavePageIds(kbName);
                return newId;
            }
            return _pageMap[kbName][^1];
        }
    }

    private void LoadPageIds(string kbName)
    {
        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;
        var page = bpm.FetchPage(0);
        if (page == null) return;

        try
        {
            var ids = new List<int>();
            int offset = 1024;
            int count = BitConverter.ToInt32(page.Data, offset);
            if (count > 0 && count < 1000)
            {
                for (int i = 0; i < count; i++)
                {
                    int id = BitConverter.ToInt32(page.Data, offset + 4 + (i * 4));
                    if (id > 0) ids.Add(id);
                }
            }
            _pageMap[kbName] = ids;
        }
        catch { }
        finally { bpm.UnpinPage(0, false); }
    }

    private void SavePageIds(string kbName)
    {
        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;
        var page = bpm.FetchPage(0);
        if (page == null) return;

        try
        {
            int offset = 1024;
            var ids = _pageMap[kbName];
            BitConverter.GetBytes(ids.Count).CopyTo(page.Data, offset);
            for (int i = 0; i < ids.Count; i++)
            {
                BitConverter.GetBytes(ids[i]).CopyTo(page.Data, offset + 4 + (i * 4));
            }
            bpm.UnpinPage(0, true);
            bpm.FlushPage(0);
        }
        catch { bpm.UnpinPage(0, false); }
    }

    // In-memory page mapping: kbName -> list of page IDs for concepts
    private readonly Dictionary<string, List<int>> _pageMap = new();

    public int TotalCatalogPages { get { lock (_lock) { return _pageMap.Values.Sum(v => v.Count); } } }
}
