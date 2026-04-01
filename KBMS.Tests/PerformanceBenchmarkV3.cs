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
        _storagePool = new StoragePool(_tempDir, 1024); // 1024 pages buffer
        _data = new V3DataRouter(_storagePool);
    }

    public void Dispose()
    {
        _storagePool.Dispose();
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void V3_Engine_Extreme_Stress_Benchmark()
    {
        const int insertCount = 100000;
        const string kbName = "ExtremeKB";
        const string conceptName = "BigData";

        _output.WriteLine($"Starting V3 EXTREME Stress Benchmark: {insertCount} Inserts...");

        // 1. Benchmark INSERT (100k)
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < insertCount; i++)
        {
            _data.InsertObject(kbName, new ObjectInstance
            {
                Id = Guid.NewGuid(),
                ConceptName = conceptName,
                Values = new Dictionary<string, object>
                {
                    ["id"] = i,
                    ["val"] = $"Value_{i}",
                    ["status"] = i % 10 == 0 ? "ERROR" : "OK"
                }
            });
        }
        sw.Stop();
        _output.WriteLine($"INSERT {insertCount} objects: {sw.ElapsedMilliseconds}ms ({(insertCount * 1000.0 / sw.ElapsedMilliseconds):F2} ops/sec)");

        // 2. Full Scan
        sw.Restart();
        var all = _data.SelectObjects(kbName, conceptName);
        sw.Stop();
        _output.WriteLine($"SELECT ALL {all.Count} objects: {sw.ElapsedMilliseconds}ms");

        // 3. Filter
        sw.Restart();
        var errors = _data.SelectObjects(kbName, conceptName, v => v["status"].ToString() == "ERROR");
        sw.Stop();
        _output.WriteLine($"FILTER (status=='ERROR') - Found {errors.Count} objects: {sw.ElapsedMilliseconds}ms");

        // 4. JOIN Benchmark (20k x 20k)
        _output.WriteLine("\nStarting JOIN Benchmark (10,000 x 10,000)...");
        for (int i = 0; i < 10000; i++)
        {
            _data.InsertObject(kbName, new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "A", Values = new Dictionary<string, object> { ["key"] = i, ["data"] = "A" } });
            _data.InsertObject(kbName, new ObjectInstance { Id = Guid.NewGuid(), ConceptName = "B", Values = new Dictionary<string, object> { ["key"] = i, ["data"] = "B" } });
        }
        
        sw.Restart();
        // Simulate a Hash Join logic via Router (In real KQL this uses HashJoinOperator)
        var listA = _data.SelectObjects(kbName, "A");
        var listB = _data.SelectObjects(kbName, "B");
        var joined = from a in listA
                     join b in listB on a.Values["key"].ToString() equals b.Values["key"].ToString()
                     select new { a, b };
        var results = joined.ToList();
        sw.Stop();
        _output.WriteLine($"JOIN 10k x 10k: {sw.ElapsedMilliseconds}ms (Total results: {results.Count})");

        Assert.Equal(insertCount, all.Count);
        Assert.Equal(10000, results.Count);
    }
}
