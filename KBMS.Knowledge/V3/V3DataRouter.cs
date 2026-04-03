using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KBMS.Models;
using KBMS.Parser.Ast.Kql;
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
    private readonly StoragePool _storagePool;
    
    // Per-database optimizers (RC10: ensure query pushdown uses the correct buffer pool)
    private readonly Dictionary<string, QueryOptimizer> _kbOptimizers = new();
    
    // Catalog: "kbName:conceptName" -> list of physical page IDs holding that concept's data
    private readonly Dictionary<string, List<int>> _pageCatalog = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _catalogLock = new();
    
    // In-Memory Value Index: "kbName:conceptName" -> (FieldName -> (Value -> (PageId, SlotId)))
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, (int PageId, int SlotId)>>> _valueIndex = new(StringComparer.OrdinalIgnoreCase);

    public V3DataRouter(StoragePool storagePool)
    {
        _storagePool = storagePool;
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
            var managers = _storagePool.GetManagers(kbName);
            var bpm = managers.Bpm;
            var diskManager = managers.Disk;

            var tuple = ObjectToTuple(obj);
            var data = tuple.Serialize();
            var catalogKey = $"{kbName}:{obj.ConceptName}";

            lock (_catalogLock)
            {
                if (!_pageCatalog.ContainsKey(catalogKey)) LoadCatalog(kbName);
                
                var pageId = GetOrAllocateWritablePage(kbName, catalogKey, data.Length);
                var page = bpm.FetchPage(pageId);
                if (page == null) return false;

                // --- WAL LOGGING ---
                var wal = managers.Wal;
                var txnId = wal.Begin();
                // -------------------

                var slottedPage = new SlottedPage(page);
                if (slottedPage.TupleCount == 0 && slottedPage.FreeSpacePointer == 0)
                    slottedPage.Init(pageId);
                var slotId = slottedPage.InsertTuple(data);

                if (slotId < 0)
                {
                    bpm.UnpinPage(page.PageId, false);
                    var newPageId = diskManager.AllocatePage();
                    if (!_pageCatalog.ContainsKey(catalogKey)) _pageCatalog[catalogKey] = new List<int>();
                    _pageCatalog[catalogKey].Add(newPageId);
                    SaveCatalog(kbName);

                    page = bpm.FetchPage(newPageId);
                    if (page == null) return false;
                    
                    slottedPage = new SlottedPage(page);
                    slottedPage.Init(newPageId);
                    slotId = slottedPage.InsertTuple(data);
                }

                // --- WAL LOG (Row-Level) ---
                wal.LogInsert(txnId, page.PageId, slotId, data);
                // ---------------------------

                // --- UPDATE VALUE INDEX (specifically for 'id' field) ---
                if (obj.Values.TryGetValue("id", out var val))
                {
                    if (!_valueIndex.ContainsKey(catalogKey)) _valueIndex[catalogKey] = new();
                    if (!_valueIndex[catalogKey].ContainsKey("id")) _valueIndex[catalogKey]["id"] = new();
                    _valueIndex[catalogKey]["id"][val.ToString()!] = (page.PageId, slotId);
                }
                // --------------------

                // --- WAL COMMIT ---
                wal.Commit(txnId);
                // ------------------

                bpm.UnpinPage(page.PageId, true);
                return slotId >= 0;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[V3DataRouter] InsertObject failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// TRANSACTIONAL BULK INSERT: Inserts a batch of objects within a single WAL transaction.
    /// This is the most efficient way to load large datasets.
    /// </summary>
    public int BulkInsertObjects(string kbName, List<ObjectInstance> objects)
    {
        if (objects == null || objects.Count == 0) return 0;

        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;
        var diskManager = managers.Disk;
        var wal = managers.Wal;

        var txnId = wal.Begin();
        int successCount = 0;

        try
        {
            lock (_catalogLock)
            {
                foreach (var obj in objects)
                {
                    var tuple = ObjectToTuple(obj);
                    var data = tuple.Serialize();
                    var catalogKey = $"{kbName}:{obj.ConceptName}";

                    if (!_pageCatalog.ContainsKey(catalogKey)) LoadCatalog(kbName);
                    
                    var pageId = GetOrAllocateWritablePage(kbName, catalogKey, data.Length);
                    var page = bpm.FetchPage(pageId);
                    if (page == null) continue;

                    var slottedPage = new SlottedPage(page);
                    if (slottedPage.TupleCount == 0 && slottedPage.FreeSpacePointer == 0)
                        slottedPage.Init(pageId);
                        
                    var slotId = slottedPage.InsertTuple(data);

                    if (slotId < 0)
                    {
                        bpm.UnpinPage(page.PageId, false);
                        var newPageId = diskManager.AllocatePage();
                        if (!_pageCatalog.ContainsKey(catalogKey)) _pageCatalog[catalogKey] = new List<int>();
                        _pageCatalog[catalogKey].Add(newPageId);
                        SaveCatalog(kbName);

                        page = bpm.FetchPage(newPageId);
                        if (page == null) continue;
                        
                        slottedPage = new SlottedPage(page);
                        slottedPage.Init(newPageId);
                        slotId = slottedPage.InsertTuple(data);
                    }

                    // Log each insertion under the SAME txnId
                    wal.LogInsert(txnId, page.PageId, slotId, data);

                    // Update Index
                    if (!_valueIndex.ContainsKey(catalogKey)) _valueIndex[catalogKey] = new();
                    if (!_valueIndex[catalogKey].ContainsKey("id")) _valueIndex[catalogKey]["id"] = new();
                    if (obj.Values.TryGetValue("id", out var idVal))
                        _valueIndex[catalogKey]["id"][idVal.ToString()!] = (page.PageId, slotId);

                    bpm.UnpinPage(page.PageId, true);
                    successCount++;
                }
            }

            wal.Commit(txnId);
            return successCount;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[V3DataRouter] BulkInsert failed: {ex.Message}");
            return successCount;
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
            if (!_pageCatalog.ContainsKey(catalogKey)) LoadCatalog(kbName);
            if (!_pageCatalog.TryGetValue(catalogKey, out var ids)) return results;
            pageIds = new List<int>(ids);
        }

        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;

        foreach (var pageId in pageIds)
        {
            var page = bpm.FetchPage(pageId);
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

            bpm.UnpinPage(page.PageId, false);
        }

        return results;
    }

    /// <summary>
    /// FAST PATH: Uses the in-memory value index to find an object by a specific field value.
    /// Eliminates the need for O(N) full table scans.
    /// </summary>
    public List<ObjectInstance> SelectByValue(string kbName, string conceptName, string fieldName, string value)
    {
        var results = new List<ObjectInstance>();
        var catalogKey = $"{kbName}:{conceptName}";
        
        if (!_valueIndex.TryGetValue(catalogKey, out var fieldIndex)) return results;
        if (!fieldIndex.TryGetValue(fieldName, out var valMap)) return results;
        if (!valMap.TryGetValue(value, out var pos)) return results;

        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;
        var page = bpm.FetchPage(pos.PageId);
        if (page == null) return results;

        try
        {
            var sp = new SlottedPage(page);
            var raw = sp.GetTuple(pos.SlotId);
            if (raw != null)
            {
                var tuple = Tuple.Deserialize(raw);
                results.Add(TupleToObject(tuple, conceptName, kbName));
            }
        }
        finally
        {
            bpm.UnpinPage(pos.PageId, false);
        }

        return results;
    }
    
    /// <summary>
    /// Executes a SELECT query using the Query Optimizer and Volcano Execution Pipeline.
    /// This is the "high-performance" path that supports joins and predicate pushdown.
    /// </summary>
    public List<ObjectInstance> ExecuteSelect(string kbName, SelectNode node, Concept? concept = null)
    {
        var results = new List<ObjectInstance>();
        
        QueryOptimizer optimizer;
        lock (_kbOptimizers)
        {
            if (!_kbOptimizers.TryGetValue(kbName, out optimizer!))
            {
                var managers = _storagePool.GetManagers(kbName);
                optimizer = new QueryOptimizer(managers.Bpm, GetConceptPageIds);
                _kbOptimizers[kbName] = optimizer;
            }
        }

        var plan = optimizer.BuildExecutionPlan(node, kbName);

        try
        {
            plan.Init();
            while (true)
            {
                var tuple = plan.Next();
                if (tuple == null) break;

                // Convert binary tuple back to V1 ObjectInstance for compatibility with UI/CLI
                var obj = TupleToObject(tuple, node.ConceptName, kbName, concept);
                results.Add(obj);
            }
        }
        finally
        {
            plan.Close();
            plan.Dispose();
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

        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;

        var wal = managers.Wal;

        foreach (var pageId in pageIds)
        {
            var page = bpm.FetchPage(pageId);
            if (page == null) continue;

            // --- WAL LOGGING (Start Delete part of Update) ---
            var txnId = wal.Begin();
            byte[] beforeImage = new byte[Page.PAGE_SIZE];
            Array.Copy(page.Data, beforeImage, Page.PAGE_SIZE);
            // --------------------------------------------------

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
                    
                    // --- WAL COMMIT (Delete part) ---
                    byte[] afterImage = new byte[Page.PAGE_SIZE];
                    Array.Copy(page.Data, afterImage, Page.PAGE_SIZE);
                    wal.LogWrite(txnId, page.PageId, beforeImage, afterImage);
                    wal.Commit(txnId);
                    // --------------------------------

                    bpm.UnpinPage(page.PageId, true);

                    // Re-insert with updated values (This will have its own WAL txn)
                    var updated = new ObjectInstance { Id = id, ConceptName = conceptName, Values = newValues };
                    return InsertObject(kbName, updated);
                }
            }

            bpm.UnpinPage(page.PageId, false);
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

        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;

        int deleted = 0;
        var wal = managers.Wal;

        foreach (var pageId in pageIds)
        {
            var page = bpm.FetchPage(pageId);
            if (page == null) continue;

            // --- WAL LOGGING (Start) ---
            var txnId = wal.Begin();
            byte[] beforeImage = new byte[Page.PAGE_SIZE];
            Array.Copy(page.Data, beforeImage, Page.PAGE_SIZE);
            // ---------------------------

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

            if (anyDeleted)
            {
                // --- WAL COMMIT ---
                byte[] afterImage = new byte[Page.PAGE_SIZE];
                Array.Copy(page.Data, afterImage, Page.PAGE_SIZE);
                wal.LogWrite(txnId, page.PageId, beforeImage, afterImage);
                wal.Commit(txnId);
                // ------------------
            }

            bpm.UnpinPage(page.PageId, anyDeleted);
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

    private int GetOrAllocateWritablePage(string kbName, string catalogKey, int dataSize)
    {
        var managers = _storagePool.GetManagers(kbName);
        var diskManager = managers.Disk;

        lock (_catalogLock)
        {
            if (!_pageCatalog.ContainsKey(catalogKey)) LoadCatalog(kbName);
            if (!_pageCatalog.ContainsKey(catalogKey) || _pageCatalog[catalogKey].Count == 0)
            {
                var newPageId = diskManager.AllocatePage();
                _pageCatalog[catalogKey] = new List<int> { newPageId };
                SaveCatalog(kbName);
                return newPageId;
            }

            // Return the last page in the chain (most likely to have space)
            return _pageCatalog[catalogKey].Last();
        }
    }

    private void LoadCatalog(string kbName)
    {
        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;
        var headerPage = bpm.FetchPage(0);
        if (headerPage == null) return;

        try
        {
            int catalogRootPageId = BitConverter.ToInt32(headerPage.Data, 2048);
            if (catalogRootPageId <= 0) return;

            var catalogPage = bpm.FetchPage(catalogRootPageId);
            if (catalogPage == null) return;

            string json = Encoding.UTF8.GetString(catalogPage.Data).TrimEnd('\0');
            var diskCatalog = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<int>>>(json);
            if (diskCatalog != null)
            {
                foreach (var kv in diskCatalog)
                {
                    _pageCatalog[kv.Key] = kv.Value;
                }
            }
            bpm.UnpinPage(catalogRootPageId, false);
        }
        catch { }
        finally { bpm.UnpinPage(0, false); }
    }

    private void SaveCatalog(string kbName)
    {
        var managers = _storagePool.GetManagers(kbName);
        var bpm = managers.Bpm;
        var diskManager = managers.Disk;
        var headerPage = bpm.FetchPage(0);
        if (headerPage == null) return;

        try
        {
            int catalogRootPageId = BitConverter.ToInt32(headerPage.Data, 2048);
            if (catalogRootPageId <= 0)
            {
                catalogRootPageId = diskManager.AllocatePage();
                BitConverter.GetBytes(catalogRootPageId).CopyTo(headerPage.Data, 2048);
                bpm.UnpinPage(0, true);
            }
            else
            {
                bpm.UnpinPage(0, false);
            }

            var catalogPage = bpm.FetchPage(catalogRootPageId);
            if (catalogPage == null) return;

            // Simple snapshot of entries for this KB
            var snapshot = _pageCatalog.Where(kv => kv.Key.StartsWith(kbName + ":")).ToDictionary(kv => kv.Key, kv => kv.Value);
            string json = System.Text.Json.JsonSerializer.Serialize(snapshot);
            byte[] data = Encoding.UTF8.GetBytes(json);
            
            Array.Clear(catalogPage.Data, 0, Page.PAGE_SIZE);
            Array.Copy(data, 0, catalogPage.Data, 0, Math.Min(data.Length, Page.PAGE_SIZE));
            
            bpm.UnpinPage(catalogRootPageId, true);
        }
        catch { try { bpm.UnpinPage(0, false); } catch {} }
    }

    public List<int> GetConceptPageIds(string kbName, string conceptName)
    {
        var catalogKey = $"{kbName}:{conceptName}";
        lock (_catalogLock)
        {
            if (!_pageCatalog.ContainsKey(catalogKey)) LoadCatalog(kbName);
            return _pageCatalog.TryGetValue(catalogKey, out var ids) ? new List<int>(ids) : new List<int>();
        }
    }

    public int GetConceptPageCount(string kbName, string conceptName)
    {
        var catalogKey = $"{kbName}:{conceptName}";
        lock (_catalogLock)
        {
            return _pageCatalog.ContainsKey(catalogKey) ? _pageCatalog[catalogKey].Count : 0;
        }
    }

    public bool ConceptExists(string kbName, string conceptName)
    {
        var catalogKey = $"{kbName}:{conceptName}";
        lock (_catalogLock)
        {
            return _pageCatalog.ContainsKey(catalogKey) && _pageCatalog[catalogKey].Count > 0;
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

    /// <summary>
    /// Convenience method to upsert a system setting into the "settings" or "version" concept.
    /// </summary>
    public bool UpdateSystemSetting(string key, string value)
    {
        if (key.Equals("Version", StringComparison.OrdinalIgnoreCase))
        {
            var existingVer = SelectObjects("system", "version");
            if (existingVer.Count > 0)
            {
                var obj = existingVer[0];
                obj.Values["version_string"] = value;
                obj.Values["build_date"] = DateTime.Now.ToString("yyyy-MM-dd");
                UpdateObject("system", "version", obj.Id, obj.Values);
            }
            else
            {
                var obj = new ObjectInstance { ConceptName = "version" };
                obj.Values["version_string"] = value;
                obj.Values["build_date"] = DateTime.Now.ToString("yyyy-MM-dd");
                InsertObject("system", obj);
            }
            return true;
        }

        // Generic settings
        var existing = SelectObjects("system", "settings", o => o.ContainsKey("variable_name") && o["variable_name"]?.ToString() == key);
        if (existing.Count > 0)
        {
            var obj = existing[0];
            obj.Values["variable_value"] = value;
            UpdateObject("system", "settings", obj.Id, obj.Values);
        }
        else
        {
            var obj = new ObjectInstance { ConceptName = "settings" };
            obj.Values["variable_name"] = key;
            obj.Values["variable_value"] = value;
            InsertObject("system", obj);
        }
        return true;
    }

    public bool DropAllMappings(string kbName)
    {
        lock (_catalogLock)
        {
            var keysToRemove = _pageCatalog.Keys.Where(k => k.StartsWith(kbName + ":")).ToList();
            foreach (var key in keysToRemove)
            {
                _pageCatalog.Remove(key);
            }
            
            // Persist the empty state for this KB (tombstone)
            SaveCatalog(kbName);
            return true;
        }
    }
}
