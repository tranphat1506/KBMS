using System;
using System.IO;
using System.Linq;
using Xunit;
using KBMS.Storage.V3;
using KBMS.Models;

namespace KBMS.Tests;

/// <summary>
/// Layer 2: Schema Catalog V3 Tests
/// Verifies ConceptCatalog and KbCatalog binary storage replace V1 Engine.cs metadata methods.
/// Each test is fully isolated using a temp file database.
/// </summary>
public class SchemaV3Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly StoragePool _storagePool;
    private readonly ConceptCatalog _conceptCatalog;
    private readonly KbCatalog _kbCatalog;

    public SchemaV3Tests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _storagePool = new StoragePool(_tempDir, 64);
        _conceptCatalog = new ConceptCatalog(_storagePool);
        _kbCatalog = new KbCatalog(_storagePool);
    }

    public void Dispose()
    {
        _storagePool?.Dispose();
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    // ========== KB Catalog Tests ==========

    [Fact]
    public void KbCatalog_Create_And_Load()
    {
        var kb = _kbCatalog.CreateKb("SchoolDB", Guid.NewGuid(), "Test school database");

        Assert.NotNull(kb);
        Assert.NotEqual(Guid.Empty, kb.Id);

        var loaded = _kbCatalog.LoadKb("SchoolDB");
        Assert.NotNull(loaded);
        Assert.Equal("SchoolDB", loaded!.Name);
        Assert.Equal("Test school database", loaded.Description);
    }

    [Fact]
    public void KbCatalog_ListKbs_ReturnsAll()
    {
        _kbCatalog.CreateKb("KB1", Guid.NewGuid());
        _kbCatalog.CreateKb("KB2", Guid.NewGuid());
        _kbCatalog.CreateKb("KB3", Guid.NewGuid());

        var all = _kbCatalog.ListKbs();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void KbCatalog_Drop_RemovesKb()
    {
        _kbCatalog.CreateKb("DropMe", Guid.NewGuid());
        Assert.True(_kbCatalog.KbExists("DropMe"));

        _kbCatalog.DropKb("DropMe");
        Assert.False(_kbCatalog.KbExists("DropMe"));
        Assert.Equal(0, _kbCatalog.ListKbs().Count);
    }

    // ========== Concept Catalog Tests ==========

    [Fact]
    public void ConceptCatalog_Create_And_Load()
    {
        var concept = new Concept
        {
            Id = Guid.NewGuid(),
            Name = "Student",
            Variables = new System.Collections.Generic.List<Variable>
            {
                new Variable { Name = "name", Type = "STRING" },
                new Variable { Name = "age", Type = "INT" }
            }
        };

        var ok = _conceptCatalog.CreateConcept("SchoolDB", concept);
        Assert.True(ok);

        var loaded = _conceptCatalog.LoadConcept("SchoolDB", "Student");
        Assert.NotNull(loaded);
        Assert.Equal("Student", loaded!.Name);
        Assert.Equal(2, loaded.Variables.Count);
        Assert.Equal("name", loaded.Variables[0].Name);
    }

    [Fact]
    public void ConceptCatalog_Drop_RemovesConcept()
    {
        var concept = new Concept { Id = Guid.NewGuid(), Name = "TempConcept" };
        _conceptCatalog.CreateConcept("MyKB", concept);

        Assert.NotNull(_conceptCatalog.LoadConcept("MyKB", "TempConcept"));

        _conceptCatalog.DropConcept("MyKB", "TempConcept");
        Assert.Null(_conceptCatalog.LoadConcept("MyKB", "TempConcept"));
    }

    [Fact]
    public void ConceptCatalog_Update_ChangesVariables()
    {
        var original = new Concept
        {
            Id = Guid.NewGuid(),
            Name = "Exam",
            Variables = new System.Collections.Generic.List<Variable>
            {
                new Variable { Name = "score", Type = "FLOAT" }
            }
        };
        _conceptCatalog.CreateConcept("DB1", original);

        var updated = new Concept
        {
            Id = original.Id,
            Name = "Exam",
            Variables = new System.Collections.Generic.List<Variable>
            {
                new Variable { Name = "score", Type = "FLOAT" },
                new Variable { Name = "grade", Type = "STRING" }  // Added
            }
        };
        _conceptCatalog.UpdateConcept("DB1", updated);

        var loaded = _conceptCatalog.LoadConcept("DB1", "Exam");
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Variables.Count);
        Assert.Equal("grade", loaded.Variables[1].Name);
    }
}
