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
    public event Action<object>? OnLog; // Fires with the log object (system or audit)
    
    public SystemLogger(KBMS.Knowledge.V3.V3DataRouter v3Router)
    {
        _v3Router = v3Router;
    }

    /// <summary>
    /// Logs server lifecycle events and critical background errors to system_logs.
    /// </summary>
    public void Log(string level, string sessionId, string message, string component = "Kernel", Exception? ex = null)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string levelStr = level.ToUpper().PadRight(7);
        string formattedMessage = $"[{sessionId}] [{component}] {message}";

        try 
        {
            var logObj = new ObjectInstance { ConceptName = "system_logs" };
            logObj.Values["timestamp"] = timestamp;
            logObj.Values["level"] = level;
            logObj.Values["component"] = component;
            logObj.Values["message"] = formattedMessage;

            if (ex != null)
            {
                logObj.Values["exception_type"] = ex.GetType().Name;
                logObj.Values["stack_trace"] = ex.StackTrace ?? "";
                if (ex.InnerException != null)
                {
                    logObj.Values["inner_exception"] = ex.InnerException.Message;
                }
            }
            
            OnLog?.Invoke(new { type = "SYSTEM", data = logObj.Values });

            try { _v3Router?.InsertObject("system", logObj); } catch { }
            
            // Console output
            Console.WriteLine($"[{timestamp}] [{levelStr}] {formattedMessage}");
            if (ex != null) Console.WriteLine(ex.ToString());
        }
        catch 
        {
            // Failsafe
            Console.WriteLine($"[{timestamp}] [{levelStr}] {formattedMessage}");
        }
    }

    public void LogRequest(string sessionId, string command, string? username = null)
    {
        var userPrefix = username != null ? $"[{username}] " : "";
        Log("Info", sessionId, $"{userPrefix}REQUEST: {command}", "Network");
    }

    public void LogResponse(string sessionId, string type, string content = "", int? length = null)
    {
        var sizeInfo = length.HasValue ? $" ({length} bytes)" : "";
        var contentInfo = !string.IsNullOrEmpty(content) ? $" (Content: {content})" : "";
        Log("Info", sessionId, $"RESPONSE: {type}{sizeInfo}{contentInfo}", "Network");
    }

    /// <summary>
    /// Logs all user incoming queries and commands for security auditing.
    /// </summary>
    public void LogAudit(string username, string command, string status, string ipAddress, string role = "USER", string kb = "GLOBAL", double durationMs = 0)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        try 
        {
            var auditObj = new ObjectInstance { ConceptName = "audit_logs" };
            auditObj.Values["timestamp"] = timestamp;
            auditObj.Values["username"] = username;
            auditObj.Values["role"] = role;
            auditObj.Values["command"] = command;
            auditObj.Values["status"] = status;
            auditObj.Values["ip_address"] = ipAddress;
            auditObj.Values["kb_context"] = kb;
            auditObj.Values["duration_ms"] = durationMs;
            
            OnLog?.Invoke(new { type = "AUDIT", data = auditObj.Values });

            try { _v3Router?.InsertObject("system", auditObj); } catch { }
        }
        catch 
        {
            // Failsafe
            Console.WriteLine($"[AUDIT-FAILSAFE] [{timestamp}] {username} | {status} | {ipAddress} | {command}");
        }
    }
    
    // Convenience methods
    public void Info(string sessionId, string message, string component = "Kernel") => Log("Info", sessionId, message, component);
    public void Warning(string sessionId, string message, string component = "Kernel") => Log("Warning", sessionId, message, component);
    public void Error(string sessionId, string message, string component = "Kernel") => Log("Error", sessionId, message, component);
    public void Debug(string sessionId, string message, string component = "Kernel") => Log("Debug", sessionId, message, component);
    
    public void LogError(string sessionId, string component, Exception ex, string? customMsg = null)
    {
        Log("Error", sessionId, customMsg ?? ex.Message, component, ex);
    }
}
