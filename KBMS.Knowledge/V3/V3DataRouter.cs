using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KBMS.Models;
using KBMS.Storage.V3;
using KBMS.Knowledge.V3.Optimizer;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Knowledge.V3;

/// <summary>
/// The bridge between the V1 ObjectInstance model and the V3 Binary Page Engine.
/// Handles conversion of ObjectInstance ↔ Tuple (binary) and manages the Page Catalog
/// mapping a KB+Concept string key to a list of physical Page IDs.
/// 
/// The Page Catalog is stored in-memory (in a future release, it itself becomes a System KB Concept).
/// Layout of a V3 Tuple for an ObjectInstance:
///   Field 0: ObjectId (GUID, 16 bytes)
///   Field 1: FieldNames joined by '|' (UTF-8 string)
///   Field 2..N: Corresponding field values as UTF-8 strings
/// </summary>
public class V3DataRouter
{
    private readonly BufferPoolManager _bpm;
    private readonly DiskManager _diskManager;
    private readonly QueryOptimizer _optimizer;
    
    // Catalog: "kbName:conceptName" -> list of physical page IDs holding that concept's data
    private readonly Dictionary<string, List<int>> _pageCatalog = new();
    private readonly object _catalogLock = new();

    public V3DataRouter(BufferPoolManager bpm, DiskManager diskManager)
    {
        _bpm = bpm;
        _diskManager = diskManager;
        _optimizer = new QueryOptimizer(bpm);
    }

    // ==================== Insert Path (V1 -> V3) ====================

    /// <summary>
    /// Persists a V1 ObjectInstance into the V3 Page-based binary storage.
    /// This is the key "on-ramp" from old engine to new engine.
    /// </summary>
    public bool InsertObject(string kbName, ObjectInstance obj)
    {
        try
        {
            var tuple = ObjectToTuple(obj);
            var data = tuple.Serialize();
            var catalogKey = $"{kbName}:{obj.ConceptName}";

            lock (_catalogLock)
            {
                var pageId = GetOrAllocateWritablePage(catalogKey, data.Length);
                var page = _bpm.FetchPage(pageId);
                if (page == null) return false;

                var slottedPage = new SlottedPage(page);
                if (slottedPage.TupleCount == 0 && slottedPage.FreeSpacePointer == 0)
                    slottedPage.Init(pageId);
                var slotId = slottedPage.InsertTuple(data);

                if (slotId < 0)
                {
                    _bpm.UnpinPage(page.PageId, false);
                    var newPageId = _diskManager.AllocatePage();
                    _pageCatalog[catalogKey].Add(newPageId);

                    page = _bpm.FetchPage(newPageId);
                    if (page == null) return false;
                    slottedPage = new SlottedPage(page);
                    slottedPage.Init(newPageId);
                    slotId = slottedPage.InsertTuple(data);
                }

                _bpm.UnpinPage(page.PageId, true);
                return slotId >= 0;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[V3DataRouter] InsertObject failed: {ex.Message}");
            return false;
        }
    }

    // ==================== Select Path (V3 -> V1) ====================

    /// <summary>
    /// Scans all pages for a given KB+Concept and returns a list of matching ObjectInstances.
    /// Applies optional filter conditions using predicate pushdown before materializing.
    /// </summary>
    public List<ObjectInstance> SelectObjects(string kbName, string conceptName, Func<Dictionary<string, object>, bool>? predicate = null, Concept? concept = null)
    {
        var results = new List<ObjectInstance>();
        var catalogKey = $"{kbName}:{conceptName}";

        List<int> pageIds;
        lock (_catalogLock)
        {
            if (!_pageCatalog.TryGetValue(catalogKey, out var ids)) return results;
            pageIds = new List<int>(ids);
        }

        foreach (var pageId in pageIds)
        {
            var page = _bpm.FetchPage(pageId);
            if (page == null) continue;

            var slottedPage = new SlottedPage(page);
            
            for (int slotId = 0; slotId < slottedPage.TupleCount; slotId++)
            {
                var rawData = slottedPage.GetTuple(slotId);
                if (rawData == null || rawData.Length == 0) continue;

                var tuple = Tuple.Deserialize(rawData);
                var obj = TupleToObject(tuple, conceptName, kbName, concept);

                // Predicate Pushdown: apply filter before adding to result list
                if (predicate == null || predicate(obj.Values))
                {
                    results.Add(obj);
                }
            }

            _bpm.UnpinPage(page.PageId, false);
        }

        return results;
    }

    // ==================== Update Path ====================

    /// <summary>
    /// Updates an existing object by ID: marks old tuple deleted, inserts updated one.
    /// Returns false if the object was not found.
    /// </summary>
    public bool UpdateObject(string kbName, string conceptName, Guid id, Dictionary<string, object> newValues, Concept? concept = null)
    {
        var catalogKey = $"{kbName}:{conceptName}";
        List<int> pageIds;
        lock (_catalogLock)
        {
            if (!_pageCatalog.TryGetValue(catalogKey, out var ids)) return false;
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

                var tuple = Tuple.Deserialize(raw);
                var obj = TupleToObject(tuple, conceptName, kbName, concept);

                if (obj.Id == id)
                {
                    sp.DeleteTuple(i);
                    _bpm.UnpinPage(page.PageId, true);

                    // Re-insert with updated values
                    var updated = new ObjectInstance { Id = id, ConceptName = conceptName, Values = newValues };
                    return InsertObject(kbName, updated);
                }
            }

            _bpm.UnpinPage(page.PageId, false);
        }

        return false;
    }

    // ==================== Delete Path ====================

    /// <summary>
    /// Deletes all records in a concept matching the given predicate.
    /// Returns the number of deleted records.
    /// </summary>
    public int DeleteObjects(string kbName, string conceptName, Func<Dictionary<string, object>, bool>? predicate = null, Concept? concept = null)
    {
        var catalogKey = $"{kbName}:{conceptName}";
        List<int> pageIds;
        lock (_catalogLock)
        {
            if (!_pageCatalog.TryGetValue(catalogKey, out var ids)) return 0;
            pageIds = new List<int>(ids);
        }

        int deleted = 0;
        foreach (var pageId in pageIds)
        {
            var page = _bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            bool anyDeleted = false;

            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null) continue;

                var tuple = Tuple.Deserialize(raw);
                var obj = TupleToObject(tuple, conceptName, kbName, concept);

                if (predicate == null || predicate(obj.Values))
                {
                    sp.DeleteTuple(i);
                    deleted++;
                    anyDeleted = true;
                }
            }

            _bpm.UnpinPage(page.PageId, anyDeleted);
        }

        return deleted;
    }



    /// <summary>
    /// Converts a V1 ObjectInstance into a V3 Tuple using UTF-8 encoding.
    /// Field 0 = ID, Field 1 = '|'-separated field names, Fields 2..N = values
    /// </summary>
    private Tuple ObjectToTuple(ObjectInstance obj)
    {
        var tuple = new Tuple();
        tuple.AddGuid(obj.Id == Guid.Empty ? Guid.NewGuid() : obj.Id);
        tuple.AddString(string.Join("|", obj.Values.Keys));

        foreach (var value in obj.Values.Values)
        {
            var strValue = value switch
            {
                null => "",
                decimal dec => dec.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };
            tuple.AddString(strValue);
        }

        return tuple;
    }

    /// <summary>
    /// Reconstructs a V1 ObjectInstance from a V3 binary Tuple.
    /// Handles schema evolution by mapping legacy field order to current concept variables.
    /// </summary>
    private ObjectInstance TupleToObject(Tuple tuple, string conceptName, string kbName, Concept? concept = null)
    {
        if (tuple.Fields.Count < 2)
            return new ObjectInstance { ConceptName = conceptName };

        var id = tuple.GetGuid(0);
        var fieldNames = tuple.GetString(1).Split('|');

        var values = new Dictionary<string, object>();
        // First map what's physically in the tuple
        for (int i = 0; i < fieldNames.Length; i++)
        {
            var rawIndex = i + 2;
            if (rawIndex < tuple.Fields.Count && tuple.Fields[rawIndex].Length > 0)
            {
                var fieldName = fieldNames[i];
                var rawValue = tuple.GetString(rawIndex);
                
                // Type Casting: use concept metadata to restore native types
                var variable = concept?.Variables.FirstOrDefault(v => v.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                values[fieldName] = CastToNativeType(rawValue, variable);
            }
        }

        // Schema evolution: if we have a current concept, ensure all its variables are present.
        // If a variable is missing (newly added), add it as null.
        // If it was renamed in the concept but still has the old name in fieldNames part of the tuple,
        // we'd need more complex tracking. For now, we assume simple ADD/DROP.
        if (concept != null)
        {
            foreach (var v in concept.Variables)
            {
                if (!values.ContainsKey(v.Name))
                {
                    values[v.Name] = null!;
                }
            }
        }

        return new ObjectInstance
        {
            Id = id,
            ConceptName = conceptName,
            Values = values
        };
    }

    private object CastToNativeType(string rawValue, Variable? variable)
    {
        if (variable == null) return rawValue;

        var type = variable.Type?.ToUpperInvariant() ?? "STRING";
        try
        {
            return type switch
            {
                "INT" or "INTEGER" or "LONG" => long.TryParse(rawValue, out var l) ? l : (object)rawValue,
                "FLOAT" or "DOUBLE" or "NUMBER" => double.TryParse(rawValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : (object)rawValue,
                "BOOLEAN" or "BOOL" => bool.TryParse(rawValue, out var b) ? b : (object)rawValue,
                "DECIMAL" => decimal.TryParse(rawValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dec) ? dec : (object)rawValue,
                _ => rawValue
            };
        }
        catch { return rawValue; }
    }

    // ==================== Catalog Management ====================

    private int GetOrAllocateWritablePage(string catalogKey, int dataSize)
    {
        lock (_catalogLock)
        {
            if (!_pageCatalog.ContainsKey(catalogKey) || _pageCatalog[catalogKey].Count == 0)
            {
                var newPageId = _diskManager.AllocatePage();
                _pageCatalog[catalogKey] = new List<int> { newPageId };
                return newPageId;
            }

            // Return the last page in the chain (most likely to have space)
            return _pageCatalog[catalogKey].Last();
        }
    }

    /// <summary>
    /// The total number of distinct concepts stored in the V3 engine catalog.
    /// Useful for diagnostics and SHOW VARIABLES output.
    /// </summary>
    public Dictionary<string, List<int>> GetCatalogSnapshot()
    {
        lock (_catalogLock)
        {
            return new Dictionary<string, List<int>>(_pageCatalog);
        }
    }
}
