using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using KBMS.Storage.V3;
using KBMS.Models;
using KBMS.Knowledge.V3;

namespace KBMS.Tests;

/// <summary>
/// Layer 6: Full End-to-End Integration Tests
/// Exercises the complete V3 data path: KB creation → Concept schema → 
/// INSERT → SELECT → UPDATE → DELETE → Auth → WAL.
/// Each test uses a fully isolated temp database.
/// </summary>
public class FullIntegrationV3Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly StoragePool _storagePool;
    private readonly V3DataRouter _data;
    private readonly ConceptCatalog _concepts;
    private readonly KbCatalog _kbs;
    private readonly UserCatalog _users;

    public FullIntegrationV3Tests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _storagePool = new StoragePool(_tempDir, 256);
        
        _data = new V3DataRouter(_storagePool);
        _concepts = new ConceptCatalog(_storagePool);
        _kbs = new KbCatalog(_storagePool);
        _users = new UserCatalog(_storagePool);
    }

    public void Dispose()
    {
        _storagePool?.Dispose();
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    // =========================================================
    // Test 1: Full CRUD Lifecycle
    // =========================================================
    [Fact]
    public void Full_CRUD_Lifecycle()
    {
        // CREATE KB
        var kb = _kbs.CreateKb("University", Guid.NewGuid(), "University DB");
        Assert.NotNull(kb);

        // CREATE CONCEPT
        var studentConcept = new Concept
        {
            Id = Guid.NewGuid(),
            Name = "Student",
            Variables = new List<Variable>
            {
                new Variable { Name = "name", Type = "STRING" },
                new Variable { Name = "grade", Type = "INT" }
            }
        };
        Assert.True(_concepts.CreateConcept("University", studentConcept));

        // INSERT
        var alice = new ObjectInstance
        {
            Id = Guid.NewGuid(),
            ConceptName = "Student",
            Values = new Dictionary<string, object> { ["name"] = "Alice", ["grade"] = 95 }
        };
        Assert.True(_data.InsertObject("University", alice));

        // SELECT
        var all = _data.SelectObjects("University", "Student");
        Assert.Single(all);
        Assert.Equal("Alice", all[0].Values["name"].ToString());

        // UPDATE
        var updated = new Dictionary<string, object> { ["name"] = "Alice Wong", ["grade"] = 98 };
        Assert.True(_data.UpdateObject("University", "Student", alice.Id, updated));

        var afterUpdate = _data.SelectObjects("University", "Student");
        Assert.Single(afterUpdate);
        Assert.Equal("Alice Wong", afterUpdate[0].Values["name"].ToString());

        // DELETE
        int deleted = _data.DeleteObjects("University", "Student");
        Assert.Equal(1, deleted);

        var afterDelete = _data.SelectObjects("University", "Student");
        Assert.Empty(afterDelete);
    }

    // =========================================================
    // Test 2: Auth Guards KB Access
    // =========================================================
    [Fact]
    public void Auth_Guards_Privilege_Access()
    {
        var user = _users.CreateUser("student_user", "pass123", UserRole.USER);
        Assert.NotNull(user);
        Assert.False(user!.KbPrivileges.ContainsKey("MedicalDB"));

        _users.GrantPrivilege("student_user", "MedicalDB", Privilege.READ);
        var reloaded = _users.FindUser("student_user");
        Assert.True(reloaded!.KbPrivileges.ContainsKey("MedicalDB"));
        Assert.Equal(Privilege.READ, reloaded.KbPrivileges["MedicalDB"]);

        _users.RevokePrivilege("student_user", "MedicalDB");
        var revoked = _users.FindUser("student_user");
        Assert.False(revoked!.KbPrivileges.ContainsKey("MedicalDB"));
    }

    // =========================================================
    // Test 3: Multi-Concept JOIN Simulation
    // =========================================================
    [Fact]
    public void MultiConcept_Cross_Reference_Scan()
    {
        // Simulate joining Students and Grades
        var studentsKb = "TestDB";

        for (int i = 1; i <= 5; i++)
        {
            _data.InsertObject(studentsKb, new ObjectInstance
            {
                Id = Guid.NewGuid(), ConceptName = "Student",
                Values = new Dictionary<string, object> { ["id"] = i, ["name"] = $"Student_{i}" }
            });
        }

        for (int i = 1; i <= 5; i++)
        {
            _data.InsertObject(studentsKb, new ObjectInstance
            {
                Id = Guid.NewGuid(), ConceptName = "Exam",
                Values = new Dictionary<string, object> { ["student_id"] = i, ["score"] = i * 20 }
            });
        }

        var students = _data.SelectObjects(studentsKb, "Student");
        var exams = _data.SelectObjects(studentsKb, "Exam");

        // Simulate hash join in memory (this is what V3 HashJoinOperator does on disk)
        var joined = from s in students
                     join e in exams
                     on s.Values["id"].ToString() equals e.Values["student_id"].ToString()
                     select new { Name = s.Values["name"], Score = e.Values["score"] };

        var joinList = joined.ToList();
        Assert.Equal(5, joinList.Count);
        Assert.All(joinList, j => Assert.NotNull(j.Name));
    }

    // =========================================================
    // Test 4: EXPLAIN-style Plan Inspection
    // =========================================================
    [Fact]
    public void Optimizer_Can_Build_Scan_Plan()
    {
        var optimizer = new KBMS.Knowledge.V3.Optimizer.QueryOptimizer(_storagePool.GetManagers("University").Bpm, _data.GetConceptPageIds);

        var selectNode = new KBMS.Parser.Ast.Kql.SelectNode
        {
            ConceptName = "Product",
            Conditions = new List<KBMS.Parser.Ast.Kql.Condition>
            {
                new KBMS.Parser.Ast.Kql.Condition
                {
                    Field = "price",
                    Operator = ">",
                    Value = new KBMS.Parser.Ast.Expressions.LiteralNode { Value = 100 }
                }
            }
        };

        var plan = optimizer.ExplainSelect(selectNode, "University");
        Assert.NotNull(plan);
        
        string formatted = plan!.FormatExplain();
        // Should produce a Filter(Scan) plan tree
        Assert.Contains("Scan", formatted);
        Assert.Contains("Filter", formatted);
    }

    // =========================================================
    // Test 5: Large Dataset - 5000 Objects
    // =========================================================
    [Fact]
    public void Large_Dataset_5000_Objects()
    {
        const int n = 5000;
        for (int i = 0; i < n; i++)
        {
            _data.InsertObject("BigData", new ObjectInstance
            {
                Id = Guid.NewGuid(),
                ConceptName = "Record",
                Values = new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["value"] = $"Row_{i:D5}",
                    ["score"] = (i % 100) * 1.5
                }
            });
        }

        var all = _data.SelectObjects("BigData", "Record");
        Assert.Equal(n, all.Count);

        // Verify predicate works across pages
        var high = _data.SelectObjects("BigData", "Record",
            v => Convert.ToInt32(v["index"].ToString()) >= 4500);
        Assert.Equal(500, high.Count);
    }

    // =========================================================
    // Test 6: WAL Uncommitted Detection
    // =========================================================
    [Fact]
    public void WAL_Detects_Uncommitted_Writes_For_Recovery()
    {
        var managers = _storagePool.GetManagers("RecoveryTest");
        var wal = managers.Wal;
        var txnId = wal.Begin();

        var before = new byte[32];
        var after = new byte[32];
        after[0] = 0xDE; after[1] = 0xAD;

        wal.LogWrite(txnId, pageId: 42, before, after);
        // DO NOT commit — simulates crash

        // Open a NEW WalManagerV3 to simulate restart
        string dbPath = Path.Combine(_tempDir, "RecoveryTest.kdb");
        using var recoveryWal = new WalManagerV3(dbPath);
        var uncommitted = recoveryWal.RecoverUncommitted();

        Assert.NotEmpty(uncommitted);
        var entry = uncommitted.First(e => e.pageId == 42);
        Assert.Equal(0x00, entry.beforeImage[0]); 
    }
}
