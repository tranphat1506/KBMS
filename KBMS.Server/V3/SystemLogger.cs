using System;
using KBMS.Models;
using KBMS.Storage;

namespace KBMS.Server.V3;

/// <summary>
/// A persistent Logger that writes audit and system events natively into the `system` Knowledge Base.
/// This allows developers/DBAs to run KBQL queries like: 
/// SELECT * FROM audit_logs WHERE status = 'FAIL' IN system;
/// </summary>
public class SystemLogger
{
    private readonly KBMS.Knowledge.V3.V3DataRouter _v3Router;
    
    public SystemLogger(KBMS.Knowledge.V3.V3DataRouter v3Router)
    {
        _v3Router = v3Router;
    }

    /// <summary>
    /// Logs server lifecycle events and critical background errors to system_logs.
    /// </summary>
    public void LogSystemEvent(string level, string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        
        try 
        {
            var logObj = new ObjectInstance { ConceptName = "system_logs" };
            logObj.Values["timestamp"] = timestamp;
            logObj.Values["level"] = level;
            logObj.Values["message"] = message;
            
            _v3Router.InsertObject("system", logObj);
            
            // Console mirroring for tailing
            Console.WriteLine($"[{timestamp}] [{level.ToUpper()}] {message}");
        }
        catch 
        {
            // Failsafe if system DB cannot be accessed
            Console.WriteLine($"[{timestamp}] [{level.ToUpper()}] {message}");
        }
    }

    /// <summary>
    /// Logs all user incoming queries and commands for security auditing.
    /// </summary>
    public void LogAudit(string username, string command, string status, string ipAddress)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        try 
        {
            var auditObj = new ObjectInstance { ConceptName = "audit_logs" };
            auditObj.Values["timestamp"] = timestamp;
            auditObj.Values["username"] = username;
            auditObj.Values["command"] = command;
            auditObj.Values["status"] = status;
            auditObj.Values["ip_address"] = ipAddress;
            
            _v3Router.InsertObject("system", auditObj);
        }
        catch 
        {
            // Failsafe
            Console.WriteLine($"[AUDIT-FAILSAFE] [{timestamp}] {username} | {status} | {ipAddress} | {command}");
        }
    }
    
    // Convenience methods
    public void Info(string message) => LogSystemEvent("Info", message);
    public void Warning(string message) => LogSystemEvent("Warn", message);
    public void Error(string message) => LogSystemEvent("Error", message);
}
