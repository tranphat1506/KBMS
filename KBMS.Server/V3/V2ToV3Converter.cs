using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KBMS.Models;
using KBMS.Storage;
using KBMS.Storage.V3;
using KBMS.Knowledge.V3;

namespace KBMS.Server.V3;

public class V2ToV3Converter
{
    private readonly StoragePool _storagePool;
    private readonly KbCatalog _kbCatalog;
    private readonly ConceptCatalog _conceptCatalog;
    private readonly UserCatalog _userCatalog;
    private readonly V3DataRouter _v3Router;

    public V2ToV3Converter(
        StoragePool storagePool,
        KbCatalog kbCatalog,
        ConceptCatalog conceptCatalog,
        UserCatalog userCatalog,
        V3DataRouter v3Router)
    {
        _storagePool = storagePool;
        _kbCatalog = kbCatalog;
        _conceptCatalog = conceptCatalog;
        _userCatalog = userCatalog;
        _v3Router = v3Router;
    }

    public void Migrate(string v2DataDir, string v2EncryptionKey)
    {
        Console.WriteLine($"[Migration] Starting migration from {v2DataDir}...");
        var encryption = new Encryption(v2EncryptionKey);

        if (!Directory.Exists(v2DataDir))
        {
            Console.WriteLine($"[Migration] Source directory {v2DataDir} not found.");
            return;
        }

        // 1. Migrate Users
        MigrateUsers(v2DataDir, encryption);

        // 2. Migrate Knowledge Bases
        var kbDirs = Directory.GetDirectories(v2DataDir);
        foreach (var kbDir in kbDirs)
        {
            var kbName = Path.GetFileName(kbDir);
            if (kbName == "v3" || kbName == "logs" || kbName == "backups") continue;

            MigrateKb(kbName, kbDir, encryption);
        }

        Console.WriteLine("[Migration] All legacy data processed.");
    }

    private void MigrateUsers(string v2DataDir, Encryption encryption)
    {
        string usersPath = Path.Combine(v2DataDir, "users.kmf");
        if (!File.Exists(usersPath)) usersPath = Path.Combine(v2DataDir, "users.bin");

        if (File.Exists(usersPath))
        {
            try
            {
                var data = File.ReadAllBytes(usersPath);
                var users = BinaryFormat.Deserialize<List<User>>(data, encryption);
                
                int migrated = 0;
                foreach (var user in users)
                {
                    if (_userCatalog.FindUser(user.Username) == null)
                    {
                        _userCatalog.CreateUser(user.Username, "MIGRATED_PASSWORD_PLACEHOLDER", user.Role);
                        // Note: We can't easily migrate password hashes if the salt/algo changed, 
                        // but V2 used SHA256 as well. For now, we assume user needs to reset or keep hash.
                        // Better: implement a RawInsert in UserCatalog if we want to preserve hashes.
                        migrated++;
                    }
                }
                Console.WriteLine($"[Migration] Migrated {migrated} users.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Failed to migrate users: {ex.Message}");
            }
        }
    }

    private void MigrateKb(string kbName, string kbDir, Encryption encryption)
    {
        var metaPath = Path.Combine(kbDir, "metadata.kmf");
        if (!File.Exists(metaPath)) metaPath = Path.Combine(kbDir, "metadata.bin");

        if (!File.Exists(metaPath)) return;

        try
        {
            Console.WriteLine($"[Migration] Found Knowledge Base: {kbName}");
            
            // Skip if already in V3
            if (_kbCatalog.KbExists(kbName))
            {
                Console.WriteLine($"[Migration] KB '{kbName}' already exists in V3. Skipping.");
                return;
            }

            // Load V2 Data
            var metaData = File.ReadAllBytes(metaPath);
            var kbInfo = BinaryFormat.Deserialize<KnowledgeBase>(metaData, encryption);

            var conceptPath = Path.Combine(kbDir, "concepts.kmf");
            var conceptData = File.Exists(conceptPath) ? File.ReadAllBytes(conceptPath) : null;
            var concepts = conceptData != null ? BinaryFormat.Deserialize<List<Concept>>(conceptData, encryption) : new List<Concept>();

            var objectPath = Path.Combine(kbDir, "objects.kdf");
            var objectData = File.Exists(objectPath) ? File.ReadAllBytes(objectPath) : null;
            var objects = objectData != null ? BinaryFormat.Deserialize<List<ObjectInstance>>(objectData, encryption) : new List<ObjectInstance>();

            // Create V3 KB
            _kbCatalog.CreateKb(kbName, kbInfo.OwnerId, kbInfo.Description);

            // Import Concepts
            foreach (var concept in concepts)
            {
                _conceptCatalog.CreateConcept(kbName, concept);
            }

            // Import Objects
            int objectCount = 0;
            foreach (var obj in objects)
            {
                _v3Router.InsertObject(kbName, obj);
                objectCount++;
            }

            Console.WriteLine($"[Migration] Successfully migrated '{kbName}': {concepts.Count} concepts, {objectCount} objects.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Migration] Failed to migrate KB '{kbName}': {ex.Message}");
        }
    }
}
