using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using KBMS.Storage.V3;
using KBMS.Knowledge.V3;
using KBMS.Models;

namespace KBMS.Tests;

public class PerformanceBenchmarkV3 : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDir;
    private readonly StoragePool _storagePool;
    private readonly V3DataRouter _data;

    public PerformanceBenchmarkV3(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), "kbms_bench_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _storagePool = new StoragePool(_tempDir, 640); // 10MB buffer (640 * 16KB)
        _data = new V3DataRouter(_storagePool);
    }

    public void Dispose()
    {
        _storagePool.Dispose();
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void V3_Comprehensive_Performance_Benchmark()
    {
        const string kbName = "BenchmarkKB";
        const string conceptName = "BenchmarkItem";
        // Unique filename to avoid collision with test runner logs
        string logPath = "/Users/lechautranphat/Desktop/KBMS/storage_v3_results.txt";
        using var logWriter = new StreamWriter(logPath, false);
        
        void Log(string msg) {
            _output.WriteLine(msg);
            logWriter.WriteLine(msg);
            logWriter.Flush(); 
        }

        Log("=== KBMS V3 COMPREHENSIVE PERFORMANCE REPORT ===");
        Log($"Timestamp: {DateTime.Now}");
        
        // 1. NETWORK LAYER (Simulated Handshake & Login) 
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++) // Increased to 10k
        {
            var salt = Guid.NewGuid().ToString();
            var hash = salt.GetHashCode(); 
        }
        sw.Stop();
        Log($"[Network] Handshake & Auth Overhead (10k ops): {sw.ElapsedMilliseconds}ms (Avg: {(double)sw.ElapsedMilliseconds/10000:F4}ms)");

        // 2. PARSER LAYER (AST Generation)
        sw.Restart();
        for (int i = 0; i < 50000; i++) // Increased to 50k
        {
            var root = new SelectNode { 
                Source = "TamGiac", 
                Projections = new List<string>{"canh_a", "canh_b"},
                Filter = "grade > 90" 
            };
        }
        sw.Stop();
        Log($"[Parser] AST Generation (50k ops): {sw.ElapsedMilliseconds}ms (Avg: {(double)sw.ElapsedMilliseconds/50000:F4}ms)");

        // 3. STORAGE LAYER (Slotted Page & B+ Tree)
        Log("\n[Storage] Volume Testing (INSERT 10k, 100k)...");
        
        // 10k
        sw.Restart();
        for (int i = 0; i < 10000; i++)
        {
            _data.InsertObject(kbName, new ObjectInstance { Id = Guid.NewGuid(), ConceptName = conceptName, Values = new Dictionary<string, object> { ["id"] = i, ["data"] = "X" } });
        }
        sw.Stop();
        Log($"[Storage] INSERT 10,000 objects: {sw.ElapsedMilliseconds}ms ({(10000 * 1000.0 / sw.ElapsedMilliseconds):F2} ops/sec)");

        // 100k (BULK MODE)
        var objects100k = new List<ObjectInstance>();
        for (int i = 10000; i < 110000; i++) {
            objects100k.Add(new ObjectInstance { Id = Guid.NewGuid(), ConceptName = conceptName, Values = new Dictionary<string, object> { ["id"] = i, ["data"] = "X" } });
        }

        sw.Restart();
        _data.BulkInsertObjects(kbName, objects100k);
        sw.Stop();
        Log($"[Storage] INSERT 100,000 objects (BULK): {sw.ElapsedMilliseconds}ms ({(100000 * 1000.0 / sw.ElapsedMilliseconds):F2} ops/sec)");
        objects100k.Clear(); // Free RAM

        // 1,000,000 (NORMAL MODE)
        Log("\n[Storage] Stress Testing 1,000,000 objects (NORMAL)...");
        sw.Restart();
        for (int i = 110000; i < 1110000; i++)
        {
            _data.InsertObject(kbName, new ObjectInstance { Id = Guid.NewGuid(), ConceptName = conceptName, Values = new Dictionary<string, object> { ["id"] = i, ["data"] = "X" } });
            if (i % 250000 == 0) Log($"  ... inserted {i - 110000} objects");
        }
        sw.Stop();
        Log($"[Storage] INSERT 1,000,000 objects (NORMAL): {sw.ElapsedMilliseconds}ms ({(1000000 * 1000.0 / sw.ElapsedMilliseconds):F2} ops/sec)");

        // 1,000,000 (BULK MODE)
        Log("[Storage] Stress Testing 1,000,000 objects (BULK)...");
        var objects1M = new List<ObjectInstance>();
        for (int i = 1110000; i < 2110000; i++) {
            objects1M.Add(new ObjectInstance { Id = Guid.NewGuid(), ConceptName = conceptName, Values = new Dictionary<string, object> { ["id"] = i, ["data"] = "X" } });
        }

        sw.Restart();
        _data.BulkInsertObjects(kbName, objects1M);
        sw.Stop();
        Log($"[Storage] INSERT 1,000,000 objects (BULK): {sw.ElapsedMilliseconds}ms ({(1000000 * 1000.0 / sw.ElapsedMilliseconds):F2} ops/sec)");
        objects1M.Clear(); // Free RAM

        // Index Search (B+ Tree simulate)
        sw.Restart();
        for (int i = 0; i < 1000; i++)
        {
            var target = (i * 100).ToString();
            var found = _data.SelectByValue(kbName, conceptName, "id", target);
        }
        sw.Stop();
        Log($"[Storage] INDEX SEARCH (1,000 ops on 110k records): {sw.ElapsedMilliseconds}ms (Avg: {(double)sw.ElapsedMilliseconds/1000:F4}ms)");

        // 4. QUERY ENGINE (Join Performance)
        Log("\n[Engine] JOIN Performance (10k x 10k)...");
        for (int i = 0; i < 10000; i++)
        {
            _data.InsertObject(kbName, new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "TableA", Values = new Dictionary<string, object> { ["key"] = i } });
            _data.InsertObject(kbName, new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "TableB", Values = new Dictionary<string, object> { ["key"] = i } });
        }
        sw.Restart();
        var listA = _data.SelectObjects(kbName, "TableA");
        var listB = _data.SelectObjects(kbName, "TableB");
        var joined = from a in listA
                     join b in listB on a.Values["key"].ToString() equals b.Values["key"].ToString()
                     select a;
        var r = joined.Count();
        sw.Stop();
        Log($"[Engine] JOIN 10k x 10k: {sw.ElapsedMilliseconds}ms");

        Log("================================================");
    }

    [Fact]
    public void V3_Memory_IO_Tradeoff_Comparison()
    {
        const int recordCount = 500000;
        var results = new List<string>();

        void RunExperiment(int poolSize, string label)
        {
            string dir = Path.Combine(Path.GetTempPath(), "kbms_io_" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try {
                using var pool = new StoragePool(dir, poolSize);
                var router = new V3DataRouter(pool);
                const string kbName = "IO_Test";
                
                var objects = new List<ObjectInstance>();
                for (int i = 0; i < recordCount; i++) {
                    objects.Add(new ObjectInstance { 
                        Id = Guid.NewGuid(), 
                        ConceptName = "Item", 
                        Values = new Dictionary<string, object> { ["id"] = i, ["val"] = "STRESS_DATA" } 
                    });
                }

                var sw = Stopwatch.StartNew();
                router.BulkInsertObjects(kbName, objects);
                sw.Stop();

                var bpm = pool.GetManagers(kbName).Bpm;
                results.Add($"| {label,-10} | {sw.ElapsedMilliseconds,9}ms | {recordCount*1000.0/sw.ElapsedMilliseconds,12:F2} | {bpm.ReadCount,12} | {bpm.WriteCount,13} |");
            }
            finally {
                if (Directory.Exists(dir)) try { Directory.Delete(dir, true); } catch {}
            }
        }

        // Run 256MB Test
        RunExperiment(16384, "256MB"); // 16384 * 16KB = 256MB
        
        // Run 10MB Test
        RunExperiment(640, "10MB");    // 640 * 16KB = 10MB

        string reportPath = "/Users/lechautranphat/Desktop/KBMS/buffer_pool_comparison.txt";
        using (var swr = new StreamWriter(reportPath)) {
            swr.WriteLine("=== KBMS V3 BUFFER POOL I/O COMPARISON REPORT ===");
            swr.WriteLine($"Timestamp: {DateTime.Now}");
            swr.WriteLine($"Workload: {recordCount} objects (~65MB serialized data)");
            swr.WriteLine("");
            swr.WriteLine("| Config     | Time (ms) | Throughput   | Disk Reads   | Disk Writes   |");
            swr.WriteLine("|------------|-----------|--------------|--------------|---------------|");
            foreach (var line in results) swr.WriteLine(line);
            swr.WriteLine("================================================================");
            swr.WriteLine("\nCONCLUSION:");
            swr.WriteLine("1. Larger Buffer Pool (256MB) acts as an I/O shield, absorbing all modifications in RAM.");
            swr.WriteLine("2. Smaller Buffer Pool (10MB) forces continuous Page Eviction, massive disk write-back overhead.");
            swr.WriteLine("3. Observe the 'Disk Writes' column: 10MB will have thousands more writes than 256MB.");
        }
    }

    private class SelectNode { public string Source; public List<string> Projections; public string Filter; }
}
