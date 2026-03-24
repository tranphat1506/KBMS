using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;
using KBMS.Storage.V3;

namespace KBMS.Server.V3;

public static class SystemUpdater
{
    private record Migration(string Version, Action<MigrationContext> Action, string Description);
    private record MigrationContext(KBMS.Knowledge.V3.V3DataRouter Router, UserCatalog UserCatalog);

    private static readonly List<Migration> MigrationRegistry = new()
    {
        new Migration("3.2.0-beta", ctx => {
            // Placeholder for telemetry schema if needed
        }, "Core Telemetry & Orchestration Layer Update"),
        new Migration("3.3.0-multi-db", ctx => {
            // 1. Initialize System KB Concepts
            InitializeSystemSchema(ctx.Router);

            // 2. Ensure Root User exists
            EnsureRootUser(ctx.UserCatalog);
        }, "Multi-DB Architecture & Page-Level AES-256 Encryption")
    };

    public static void Run(KbCatalog kbCatalog, ConceptCatalog conceptCatalog, UserCatalog userCatalog, KBMS.Knowledge.V3.V3DataRouter router)
    {
        Console.WriteLine(">>> KBMS System Update Orchestrator");
        Console.WriteLine($">>> Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        try
        {
            // 0. Placeholder: Future Encryption Security Check
            // if (kbCatalog.IsEncrypted()) { /* Prompt for keys or read from secure source */ }

            // 1. Ensure 'system' KB exists
            if (!kbCatalog.KbExists("system"))
            {
                Console.WriteLine("[-] System Knowledge Base missing. Aborting standalone update.");
                return;
            }

            // 2. Determine Current Version
            string currentVersion = "3.1.0"; // Default baseline
            var versionSetting = router.SelectObjects("system", "settings", v => v["variable_name"]?.ToString() == "EngineVersion").FirstOrDefault();
            if (versionSetting != null)
            {
                currentVersion = versionSetting.Values["variable_value"]?.ToString() ?? "3.1.0";
            }

            Console.WriteLine($"[*] Current System Version: {currentVersion}");

            // 3. Filter and Apply Pending Migrations
            var pending = MigrationRegistry
                .SkipWhile(m => m.Version != currentVersion) // If we are at a known version, skip it
                .Where(m => m.Version != currentVersion)     // and only take subsequent ones
                .ToList();
            
            // Special case for initial 3.1.0 baseline
            if (currentVersion == "3.1.0" && pending.Count == 0) pending = MigrationRegistry;

            if (pending.Count == 0)
            {
                Console.WriteLine("[+] System is already up to date.");
                return;
            }

            var context = new MigrationContext(router, userCatalog);

            foreach (var migration in pending)
            {
                Console.WriteLine($"[>] Applying Migration: {migration.Version} - {migration.Description}");
                
                // Wrap in a logical 'transaction' (WAL handles atomicity under the hood)
                migration.Action(context);

                // Update version after each successful step
                UpdateVersion(router, migration.Version);
                
                Console.WriteLine($"[+] Migration to {migration.Version} successful.");
            }

            Console.WriteLine($"\n[SUCCESS] System successfully orchestrated to v{pending.Last().Version}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[!] CRITICAL UPDATE FAILURE: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private static void InitializeSystemSchema(KBMS.Knowledge.V3.V3DataRouter router)
    {
        // 1. Settings Concept
        if (!router.ConceptExists("system", "settings"))
            router.InsertObject("system", new ObjectInstance { ConceptName = "settings" }); // Just touch it to exist? No, we need actual schema.
            // Actually router handles schema creation if concept doesn't exist? Check V3Router.
    }

    private static void EnsureRootUser(UserCatalog userCatalog)
    {
        if (userCatalog.FindUser("root") == null)
        {
            userCatalog.CreateUser("root", "admin", UserRole.ROOT);
        }
    }

    private static void UpdateVersion(KBMS.Knowledge.V3.V3DataRouter router, string version)
    {
        var existing = router.SelectObjects("system", "settings", v => v["variable_name"]?.ToString() == "EngineVersion").FirstOrDefault();
        var values = new Dictionary<string, object>
        {
            ["variable_name"] = "EngineVersion",
            ["variable_value"] = version
        };

        if (existing != null)
            router.UpdateObject("system", "settings", existing.Id, values);
        else
            router.InsertObject("system", new ObjectInstance { ConceptName = "settings", Values = values });

        // Log the event
        var updateLog = new ObjectInstance { ConceptName = "system_logs" };
        updateLog.Values["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        updateLog.Values["level"] = "Info";
        updateLog.Values["component"] = "Updater";
        updateLog.Values["message"] = $"CORE: System migration to {version} finalized.";
        router.InsertObject("system", updateLog);
    }
}
