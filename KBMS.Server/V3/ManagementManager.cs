using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using KBMS.Network;
using KBMS.Models;
using KBMS.Models.V3;
using KBMS.Storage.V3;

namespace KBMS.Server.V3;

public class ManagementManager
{
    private readonly ConnectionManager _connectionManager;
    private readonly SystemLogger _sysLogger;
    private readonly KBMS.Knowledge.V3.V3DataRouter _v3Router;
    private readonly KBMS.Storage.V3.UserCatalog _userCatalog;
    private readonly ConcurrentDictionary<string, byte> _logSubscribers = new();
    private readonly ConcurrentDictionary<string, Stream> _testSubscribers = new();
    private class ManualSubscriber
    {
        public Session Session { get; set; }
    }

    private readonly ConcurrentDictionary<string, ManualSubscriber> _manualSubscribers = new();
    private readonly Process _currentProcess;

    public ManagementManager(ConnectionManager connectionManager, SystemLogger sysLogger, KBMS.Knowledge.V3.V3DataRouter v3Router, KBMS.Storage.V3.UserCatalog userCatalog)
    {
        _connectionManager = connectionManager;
        _sysLogger = sysLogger;
        _v3Router = v3Router;
        _userCatalog = userCatalog;
        _currentProcess = Process.GetCurrentProcess();
        
        _sysLogger.OnLog += BroadcastLog;
    }

    public SystemStats GetSystemStats()
    {
        _currentProcess.Refresh();
        
        var totalMemory = _currentProcess.WorkingSet64 / (1024 * 1024); // MB
        var uptime = DateTime.Now - _currentProcess.StartTime;

        var version = "3.1.0-beta";
        var versionSetting = _v3Router.SelectObjects("system", "settings", v => v["variable_name"]?.ToString() == "EngineVersion").FirstOrDefault();
        if (versionSetting != null) version = versionSetting.Values["variable_value"]?.ToString() ?? version;

        return new SystemStats
        {
            CpuUsage = 0, // Simplified
            MemoryMb = totalMemory,
            Uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
            ActiveSessions = _connectionManager.GetActiveSessionsCount(),
            EngineVersion = version
        };
    }

    public void LogTest(string sessionId, string level, string message)
    {
        switch (level.ToUpper())
        {
            case "DEBUG": _sysLogger.Debug(sessionId, message, "Diagnostics"); break;
            case "INFO": _sysLogger.Info(sessionId, message, "Diagnostics"); break;
            case "WARN": _sysLogger.Warning(sessionId, message, "Diagnostics"); break;
            case "ERROR": _sysLogger.Error(sessionId, message, "Diagnostics"); break;
        }
    }
    public void SubscribeToLogs(string clientId, Stream stream)
    {
        _testSubscribers[clientId] = stream;
    }

    public void SubscribeToLogs(string clientId, Session session = null)
    {
        if (session != null)
        {
            _manualSubscribers[clientId] = new ManualSubscriber { Session = session };
        }
        else
        {
            _logSubscribers.TryAdd(clientId, 0);
        }
    }

    public void UnsubscribeFromLogs(string clientId)
    {
        _logSubscribers.TryRemove(clientId, out _);
        _manualSubscribers.TryRemove(clientId, out _);
        _testSubscribers.TryRemove(clientId, out _);
    }

    private void BroadcastLog(object logData)
    {
        var message = new Message
        {
            Type = MessageType.LOGS_STREAM,
            Content = JsonSerializer.Serialize(logData)
        };

        foreach (var entry in _logSubscribers)
        {
            // ... (existing logic for TCP subscribers)
            var clientId = entry.Key;
            var session = _connectionManager.GetSession(clientId);
            if (session == null || session.Client == null || !session.Client.Connected)
            {
                _logSubscribers.TryRemove(clientId, out _);
                continue;
            }

            Task.Run(async () =>
            {
                try
                {
                    var stream = session.Client.GetStream();
                    await Protocol.SendMessageAsync(stream, message, session.MessageLock);
                }
                catch
                {
                    _logSubscribers.TryRemove(clientId, out _);
                }
            });
        }

        // Manual/Test Subscribers (Shared Session Locking)
        foreach (var entry in _manualSubscribers)
        {
            var clientId = entry.Key;
            var sub = entry.Value;
            var session = sub.Session;

            Task.Run(async () =>
            {
                try
                {
                    var stream = session.Client.GetStream();
                    await Protocol.SendMessageAsync(stream, message, session.MessageLock);
                }
                catch
                {
                    _manualSubscribers.TryRemove(clientId, out _);
                }
            });
        }

        // Stream-based Test Subscribers
        foreach (var entry in _testSubscribers)
        {
            var clientId = entry.Key;
            var stream = entry.Value;

            Task.Run(async () =>
            {
                try
                {
                    await Protocol.SendMessageAsync(stream, message);
                }
                catch
                {
                    _testSubscribers.TryRemove(clientId, out _);
                }
            });
        }
    }

    public List<SessionInfo> ListSessions()
    {
        return _connectionManager.GetActiveSessions().Select(s => new SessionInfo
        {
            SessionId = s.SessionId,
            Username = s.User?.Username,
            ConnectedAt = s.ConnectedAt,
            LastActivityAt = s.LastActivityAt,
            IpAddress = s.IpAddress,
            CurrentKb = s.CurrentKb
        }).ToList();
    }

    // ===================== USER MANAGEMENT =====================

    public List<User> ListUsers()
    {
        return _userCatalog.ListUsers();
    }

    public bool UpsertUser(string username, string password, UserRole role)
    {
        if (username.Equals("root", StringComparison.OrdinalIgnoreCase))
        {
            role = UserRole.ROOT;
        }

        var existing = _userCatalog.FindUser(username);
        if (existing == null)
        {
            return _userCatalog.CreateUser(username, password, role) != null;
        }
        
        existing.Role = role;
        existing.SystemAdmin = role == UserRole.ROOT;
        if (!string.IsNullOrEmpty(password))
        {
            return _userCatalog.ChangePassword(username, password);
        }
        return _userCatalog.UpdateUser(existing);
    }

    public bool DeleteUser(string username)
    {
        if (username.Equals("root", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return _userCatalog.DropUser(username);
    }

    public bool GrantPermission(string username, string kb, Privilege privilege)
    {
        return _userCatalog.GrantPrivilege(username, kb, privilege);
    }

    public bool RevokePermission(string username, string kb)
    {
        return _userCatalog.RevokePrivilege(username, kb);
    }

    // ===================== LOG ANALYSIS =====================

    public List<ObjectInstance> GetLogs(string type = "system", string user = "", string level = "", string start = "", string end = "", int limit = 100, int offset = 0)
    {
        _sysLogger.Info("Diagnostics", $"[GET_LOGS] Type: {type}, UserFilter: {user}, Level: {level}, Limit: {limit}, Offset: {offset}");
        var conceptName = type.ToLower() == "audit" ? "audit_logs" : "system_logs";
        const string kbName = "system";

        // Normalize timestamp formats from UI (YYYY-MM-DDTHH:mm) to stored format (YYYY-MM-DD HH:mm:ss)
        string startTime = start?.Replace("T", " ");
        string endTime = end?.Replace("T", " ");

        return _v3Router.SelectObjects(kbName, conceptName, values =>
        {
            bool match = true;
            
            // Text Search (UserFilter maps to message/component for system, username/command for audit)
            if (!string.IsNullOrEmpty(user))
            {
                if (type.ToLower() == "system")
                {
                    string msg = values.ContainsKey("message") ? values["message"]?.ToString() ?? "" : "";
                    string comp = values.ContainsKey("component") ? values["component"]?.ToString() ?? "" : "";
                    match &= msg.Contains(user, StringComparison.OrdinalIgnoreCase) || comp.Contains(user, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    string uname = values.ContainsKey("username") ? values["username"]?.ToString() ?? "" : "";
                    string cmd = values.ContainsKey("command") ? values["command"]?.ToString() ?? "" : "";
                    match &= uname.Contains(user, StringComparison.OrdinalIgnoreCase) || cmd.Contains(user, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Severity Level (System logs only)
            if (!string.IsNullOrEmpty(level) && type.ToLower() == "system" && values.ContainsKey("level"))
            {
                string storedLevel = values["level"]?.ToString() ?? "";
                // Support short codes from UI: INFO, WARN, ERROR mapped to Info, Warning, Error
                if (level.Equals("INFO", StringComparison.OrdinalIgnoreCase))
                    match &= storedLevel.StartsWith("Info", StringComparison.OrdinalIgnoreCase);
                else if (level.Equals("WARN", StringComparison.OrdinalIgnoreCase))
                    match &= storedLevel.StartsWith("Warn", StringComparison.OrdinalIgnoreCase);
                else if (level.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
                    match &= storedLevel.StartsWith("Err", StringComparison.OrdinalIgnoreCase);
                else
                    match &= storedLevel.Equals(level, StringComparison.OrdinalIgnoreCase);
            }

            // Time Range
            if (!string.IsNullOrEmpty(startTime) && values.ContainsKey("timestamp"))
                match &= string.Compare(values["timestamp"]?.ToString(), startTime) >= 0;

            if (!string.IsNullOrEmpty(endTime) && values.ContainsKey("timestamp"))
                match &= string.Compare(values["timestamp"]?.ToString(), endTime) <= 0;

            return match;
        })
        .OrderByDescending(v => v.Values.TryGetValue("timestamp", out var ts) ? ts?.ToString() : "")
        .Skip(offset)
        .Take(limit)
        .ToList();
    }

    // ===================== SETTINGS =====================

    public List<ObjectInstance> GetServerSettings()
    {
        return _v3Router.SelectObjects("system", "settings");
    }

    public bool UpdateSetting(string name, string value)
    {
        var existing = _v3Router.SelectObjects("system", "settings", v => v["variable_name"]?.ToString() == name).FirstOrDefault();
        if (existing != null)
        {
            existing.Values["variable_value"] = value;
            return _v3Router.UpdateObject("system", "settings", existing.Id, existing.Values);
        }
        
        var newSetting = new ObjectInstance { ConceptName = "settings" };
        newSetting.Values["variable_name"] = name;
        newSetting.Values["variable_value"] = value;
        return _v3Router.InsertObject("system", newSetting);
    }
}
