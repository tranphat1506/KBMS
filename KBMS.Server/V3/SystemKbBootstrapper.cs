using System;
using KBMS.Models;
using KBMS.Storage;

namespace KBMS.Server.V3;

/// <summary>
/// Implements the "Eat Your Own Dog Food" philosophy by bootstrapping a 'system'
/// Knowledge Base on Server startup. This acts as the System Catalog, storing
/// configuration, engine version, and persistent logs in native KBMS formats.
/// </summary>
public class SystemKbBootstrapper
{
    private readonly KBMS.Storage.V3.KbCatalog _kbCatalog;
    private readonly KBMS.Storage.V3.ConceptCatalog _conceptCatalog;
    private readonly KBMS.Knowledge.V3.V3DataRouter _v3Router;

    public SystemKbBootstrapper(
        KBMS.Storage.V3.KbCatalog kbCatalog, 
        KBMS.Storage.V3.ConceptCatalog conceptCatalog,
        KBMS.Knowledge.V3.V3DataRouter v3Router)
    {
        _kbCatalog = kbCatalog;
        _conceptCatalog = conceptCatalog;
        _v3Router = v3Router;
    }

    /// <summary>
    /// Checks if the 'system' KB exists. If it does not, it creates it along with 
    /// the required system Concepts (audit_logs, system_logs, settings, version).
    /// </summary>
    public void Bootstrap()
    {
        try
        {
            if (_kbCatalog.LoadKb("system") == null)
            {
                Console.WriteLine("[SystemBootstrapper] 'system' Knowledge Base not found. Bootstrapping...");
                _kbCatalog.CreateKb("system", Guid.Empty, "System Configuration and Logs");
                
                // 1. Audit Logs Concept
                var auditConcept = new Concept { Name = "audit_logs" };
                auditConcept.Variables.Add(new Variable { Name = "timestamp", Type = "STRING" });
                auditConcept.Variables.Add(new Variable { Name = "username", Type = "STRING" });
                auditConcept.Variables.Add(new Variable { Name = "command", Type = "STRING" });
                auditConcept.Variables.Add(new Variable { Name = "status", Type = "STRING" });
                auditConcept.Variables.Add(new Variable { Name = "ip_address", Type = "STRING" });
                _conceptCatalog.CreateConcept("system", auditConcept);

                // 2. System Logs Concept
                var sysConcept = new Concept { Name = "system_logs" };
                sysConcept.Variables.Add(new Variable { Name = "timestamp", Type = "STRING" });
                sysConcept.Variables.Add(new Variable { Name = "level", Type = "STRING" });
                sysConcept.Variables.Add(new Variable { Name = "message", Type = "STRING" });
                _conceptCatalog.CreateConcept("system", sysConcept);

                // 3. Settings Concept (Variables)
                var settingsConcept = new Concept { Name = "settings" };
                settingsConcept.Variables.Add(new Variable { Name = "variable_name", Type = "STRING" });
                settingsConcept.Variables.Add(new Variable { Name = "variable_value", Type = "STRING" });
                _conceptCatalog.CreateConcept("system", settingsConcept);

                // 4. Version Concept 
                var verConcept = new Concept { Name = "version" };
                verConcept.Variables.Add(new Variable { Name = "version_string", Type = "STRING" });
                verConcept.Variables.Add(new Variable { Name = "build_date", Type = "STRING" });
                _conceptCatalog.CreateConcept("system", verConcept);

                // 5. Seed initial data
                SeedSettingsAndVersion();
                Console.WriteLine("[SystemBootstrapper] Successfully bootstrapped 'system' KB.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Failed to bootstrap 'system' KB: {ex.Message}");
        }
    }

    private void SeedSettingsAndVersion()
    {
        var versionObj = new ObjectInstance { ConceptName = "version" };
        versionObj.Values["version_string"] = "3.0.0-rc1";
        versionObj.Values["build_date"] = DateTime.Now.ToString("yyyy-MM-dd");
        _v3Router.InsertObject("system", versionObj);

        var maxConnObj = new ObjectInstance { ConceptName = "settings" };
        maxConnObj.Values["variable_name"] = "max_connections";
        maxConnObj.Values["variable_value"] = "1000";
        _v3Router.InsertObject("system", maxConnObj);

        var pageSizeObj = new ObjectInstance { ConceptName = "settings" };
        pageSizeObj.Values["variable_name"] = "page_size";
        pageSizeObj.Values["variable_value"] = "16384";
        _v3Router.InsertObject("system", pageSizeObj);
    }
}
