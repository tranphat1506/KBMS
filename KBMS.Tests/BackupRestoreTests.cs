using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using KBMS.Knowledge;
using KBMS.Models;
using KBMS.Parser;
using KBMS.Parser.Ast;
using KBMS.Parser.Ast.Kql;
using KBMS.Storage.Core;
using KBMS.Knowledge.Core;

namespace KBMS.Tests
{
    public class BackupRestoreTests : IDisposable
    {
        private readonly KnowledgeManager _km;
        private readonly User _rootUser;
        private readonly string _dataDir;

        public BackupRestoreTests()
        {
            _dataDir = Path.Combine(Path.GetTempPath(), "KBMS_BackupRestoreTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_dataDir);

            _km = InitializeKnowledgeManager(_dataDir);
            _rootUser = new User { Id = Guid.NewGuid(), Username = "root", Role = UserRole.ROOT, SystemAdmin = true };
        }

        private KnowledgeManager InitializeKnowledgeManager(string path)
        {
            var storagePool = new StoragePool(path);
            var kbCatalog = new KbCatalog(storagePool);
            var conceptCatalog = new ConceptCatalog(storagePool);
            var v3Router = new StorageRouter(storagePool);
            var userCatalog = new UserCatalog(storagePool);

            return new KnowledgeManager(storagePool, kbCatalog, conceptCatalog, userCatalog, v3Router);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dataDir))
                Directory.Delete(_dataDir, true);
        }

        private object Exec(string query, string? kbName = null)
        {
            var parser = new KBMS.Parser.Parser(query);
            var stmts = parser.ParseAll();
            object lastResult = null;
            foreach (var stmt in stmts)
            {
                lastResult = _km.Execute(stmt, _rootUser, kbName);
                if (lastResult is ErrorResponse err) throw new Exception(err.Message);
            }
            return lastResult;
        }

        [Fact]
        public void TestFullBackupAndRestore()
        {
            string kbName = "TestBackupKB";
            string backupFile = Path.Combine(_dataDir, "full_backup.kql");

            // 1. Setup KB with various components
            Exec($"CREATE KNOWLEDGE BASE {kbName};");
            Exec($"USE {kbName};");
            
            Exec("CREATE CONCEPT SinhVien (VARIABLES (MaSV: STRING, TenSV: STRING, Tuoi: INT));", kbName);
            Exec("INSERT INTO SinhVien VARIABLES (MaSV: 'SV001', TenSV: 'Nguyen Van A', Tuoi: 20);", kbName);
            Exec("INSERT INTO SinhVien VARIABLES (MaSV: 'SV002', TenSV: 'Tran Thi B', Tuoi: 21);", kbName);
            
            Exec("CREATE RULE RuleTuoi SCOPE SinhVien IF SinhVien.Tuoi > 18 THEN SinhVien.GhiChu = 'Adult';", kbName);
            
            Exec("CREATE TRIGGER TrigInsert ( ON ( INSERT OF SinhVien ), DO ( SELECT 'Trigger Fired' ) )", kbName);

            // 2. Export to KQL
            Exec($"EXPORT(KNOWLEDGE BASE: *, FORMAT: KQL, FILE: '{backupFile}')", kbName);

            Assert.True(File.Exists(backupFile));
            string backupContent = File.ReadAllText(backupFile);
            Console.WriteLine("BACKUP CONTENT:");
            Console.WriteLine(backupContent);
            Assert.Contains("CREATE CONCEPT SinhVien", backupContent);
            Assert.Contains("INSERT BULK INTO SinhVien", backupContent);
            Assert.Contains("CREATE RULE RuleTuoi", backupContent);
            Assert.Contains("CREATE TRIGGER TrigInsert", backupContent);

            // 3. Modify data in the existing KB to see it being overwritten
            Exec("DELETE FROM SinhVien WHERE MaSV = 'SV001';", kbName);
            
            // 4. Import the backup
            Exec($"IMPORT(KNOWLEDGE BASE: *, FORMAT: KQL, FILE: '{backupFile}')", kbName);

            // 5. Verify Restore
            var result = Exec("SELECT * FROM SinhVien;", kbName) as QueryResultSet;
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            
            var sv1 = result.Objects.FirstOrDefault(o => o.Values["MaSV"].ToString() == "SV001");
            Assert.NotNull(sv1);
            Assert.Equal("Nguyen Van A", sv1.Values["TenSV"].ToString());
            
            // Verify Rule
            var rules = Exec("SHOW RULES;", kbName) as QueryResultSet;
            Assert.Contains(rules.Objects, r => r.Values["Name"].ToString() == "RuleTuoi");

            // Verify Trigger
            var kb = _km.ListKbs().FirstOrDefault(k => k.Name == kbName);
            Assert.Contains(kb.Triggers, t => t.Name == "TrigInsert");

            // Setup Hierarchy and test export/import again with it
            Exec("CREATE CONCEPT Teacher (VARIABLES (MaGV: STRING));", kbName);
            Exec("ADD HIERARCHY SinhVien ISA Teacher;", kbName);
            
            Exec($"EXPORT(KNOWLEDGE BASE: *, FORMAT: KQL, FILE: '{backupFile}')", kbName);
            Console.WriteLine("SECOND BACKUP:");
            Console.WriteLine(File.ReadAllText(backupFile));
            Exec($"IMPORT(KNOWLEDGE BASE: *, FORMAT: KQL, FILE: '{backupFile}')", kbName);

            var hierarchies = Exec("SHOW HIERARCHIES;", kbName) as QueryResultSet;
            Assert.Contains(hierarchies.Objects, h => h.Values["ChildConcept"].ToString() == "SinhVien" && h.Values["ParentConcept"].ToString() == "Teacher");

            // Final check for Relations, Operators, Functions
            Exec("CREATE RELATION Than DOMAIN SinhVien RANGE SinhVien;", kbName);
            Exec("CREATE OPERATOR + (INT, INT) RETURNS INT BODY 'return arg1 + arg2;';", kbName);
            Exec("CREATE FUNCTION GetDouble (INT x) RETURNS INT BODY 'return x * 2;';", kbName);

            Exec($"EXPORT(KNOWLEDGE BASE: *, FORMAT: KQL, FILE: '{backupFile}')", kbName);
            Exec($"IMPORT(KNOWLEDGE BASE: *, FORMAT: KQL, FILE: '{backupFile}')", kbName);

            Assert.Contains((Exec("SHOW RELATIONS;", kbName) as QueryResultSet).Objects, r => r.Values["Name"].ToString() == "Than");
            Assert.Contains((Exec("SHOW OPERATORS;", kbName) as QueryResultSet).Objects, o => o.Values["Symbol"].ToString() == "+");
            Assert.Contains((Exec("SHOW FUNCTIONS;", kbName) as QueryResultSet).Objects, f => f.Values["Name"].ToString() == "GetDouble");
        }
    }
}
