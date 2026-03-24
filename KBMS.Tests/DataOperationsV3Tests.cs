using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using KBMS.Storage.V3;
using KBMS.Models;
using KBMS.Knowledge.V3;

namespace KBMS.Tests;

/// <summary>
/// Layer 3: Data Operations V3 Tests
/// Covers INSERT, SELECT (with predicate), UPDATE, DELETE through V3DataRouter.
/// Each test runs on a fresh temp database.
/// </summary>
public class DataOperationsV3Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly StoragePool _storagePool;
    private readonly V3DataRouter _router;
    private const string KB = "TestKB";
    private const string CONCEPT = "Student";

    public DataOperationsV3Tests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _storagePool = new StoragePool(_tempDir, 128);
        _router = new V3DataRouter(_storagePool);
    }

    public void Dispose()
    {
        _storagePool?.Dispose();
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    private ObjectInstance MakeStudent(string name, int age) => new ObjectInstance
    {
        Id = Guid.NewGuid(),
        ConceptName = CONCEPT,
        Values = new Dictionary<string, object> { ["name"] = name, ["age"] = age }
    };

    // ======= Test 1: Basic Insert + Select =======

    [Fact]
    public void Insert_And_Select_RoundTrip()
    {
        for (int i = 1; i <= 5; i++)
            _router.InsertObject(KB, MakeStudent($"Student_{i}", 20 + i));

        var results = _router.SelectObjects(KB, CONCEPT);
        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(r.Values.ContainsKey("name")));
    }

    // ======= Test 2: Select with Predicate =======

    [Fact]
    public void Select_WithPredicatePushdown()
    {
        for (int i = 1; i <= 10; i++)
            _router.InsertObject(KB, MakeStudent($"S{i}", i * 5)); // ages: 5, 10, 15, ... 50

        // Select where age > 25 => students 6–10 (ages 30,35,40,45,50)
        var results = _router.SelectObjects(KB, CONCEPT, values =>
        {
            var age = Convert.ToInt32(values["age"].ToString());
            return age > 25;
        });

        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(Convert.ToInt32(r.Values["age"].ToString()) > 25));
    }

    // ======= Test 3: Update Changes Field =======

    [Fact]
    public void Update_ChangesFieldValue()
    {
        var student = MakeStudent("Alice", 21);
        _router.InsertObject(KB, student);

        var updatedValues = new Dictionary<string, object> { ["name"] = "Alice Updated", ["age"] = 22 };
        bool updated = _router.UpdateObject(KB, CONCEPT, student.Id, updatedValues);
        Assert.True(updated);

        var all = _router.SelectObjects(KB, CONCEPT);
        Assert.Single(all);
        Assert.Equal("Alice Updated", all[0].Values["name"].ToString());
        Assert.Equal("22", all[0].Values["age"].ToString());
    }

    // ======= Test 4: Delete Removes Row =======

    [Fact]
    public void Delete_RemovesMatchingRows()
    {
        _router.InsertObject(KB, MakeStudent("Alice", 20));
        _router.InsertObject(KB, MakeStudent("Bob", 30));
        _router.InsertObject(KB, MakeStudent("Carol", 40));

        int deleted = _router.DeleteObjects(KB, CONCEPT, values =>
            values["name"].ToString() == "Bob");

        Assert.Equal(1, deleted);

        var remaining = _router.SelectObjects(KB, CONCEPT);
        Assert.Equal(2, remaining.Count);
        Assert.DoesNotContain(remaining, r => r.Values["name"].ToString() == "Bob");
    }

    // ======= Test 5: Multi-Page Insert Overflow =======

    [Fact]
    public void Insert_MultiPage_Overflow()
    {
        // Each student record is ~64 bytes. 16KB page ÷ 64 ≈ 200 records per page.
        // With 500 students, we need at least 2-3 pages.
        for (int i = 0; i < 500; i++)
            _router.InsertObject(KB, MakeStudent($"Student_{i:D4}", 18 + (i % 60)));

        var catalog = _router.GetCatalogSnapshot();
        Assert.True(catalog.ContainsKey($"{KB}:{CONCEPT}"));
        // Verifies multiple pages were allocated
        Assert.True(catalog[$"{KB}:{CONCEPT}"].Count >= 2);
    }

    // ======= Test 6: Select Across Multiple Pages =======

    [Fact]
    public void Select_AcrossMultiplePages_ReturnsAll()
    {
        for (int i = 0; i < 500; i++)
            _router.InsertObject(KB, MakeStudent($"Student_{i:D4}", 18));

        var all = _router.SelectObjects(KB, CONCEPT);
        Assert.Equal(500, all.Count);
    }

    // ======= Test 7: Concurrent Insert Thread Safety =======

    [Fact]
    public void Concurrent_Insert_NoDataLoss()
    {
        const int total = 200;
        var tasks = Enumerable.Range(0, 4).Select(threadId =>
            Task.Run(() =>
            {
                for (int i = 0; i < total / 4; i++)
                    _router.InsertObject(KB, MakeStudent($"T{threadId}_S{i}", 20));
            })).ToArray();

        Task.WaitAll(tasks);

        var all = _router.SelectObjects(KB, CONCEPT);
        Assert.Equal(total, all.Count);
    }
}
