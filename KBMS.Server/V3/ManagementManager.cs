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

        return new SystemStats
        {
            CpuUsage = 0, // Simplified
            MemoryMb = totalMemory,
            Uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
            ActiveSessions = _connectionManager.GetActiveSessionsCount(),
            EngineVersion = "3.1.0-beta"
        };
    }

    public void SubscribeToLogs(string clientId, Stream stream)
    {
        _logSubscribers.TryAdd(clientId, 0);
    }

    public void UnsubscribeFromLogs(string clientId)
    {
        _logSubscribers.TryRemove(clientId, out _);
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
                    await session.MessageLock.WaitAsync();
                    try
                    {
                        var stream = session.Client.GetStream();
                        await Protocol.SendMessageAsync(stream, message);
                    }
                    finally
                    {
                        session.MessageLock.Release();
                    }
                }
                catch
                {
                    // If sending fails, most likely client disconnected
                    _logSubscribers.TryRemove(clientId, out _);
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

    public List<ObjectInstance> GetLogs(string type = "system", string user = "", string level = "", string start = "", string end = "")
    {
        var conceptName = type.ToLower() == "audit" ? "audit_logs" : "system_logs";
        const string kbName = "system";

        return _v3Router.SelectObjects(kbName, conceptName, values =>
        {
            bool match = true;
            
            if (!string.IsNullOrEmpty(user) && values.ContainsKey("username"))
                match &= values["username"]?.ToString()?.Contains(user, StringComparison.OrdinalIgnoreCase) ?? false;

            if (!string.IsNullOrEmpty(level) && values.ContainsKey("level"))
                match &= values["level"]?.ToString()?.Equals(level, StringComparison.OrdinalIgnoreCase) ?? false;

            if (!string.IsNullOrEmpty(start) && values.ContainsKey("timestamp"))
                match &= string.Compare(values["timestamp"]?.ToString(), start) >= 0;

            if (!string.IsNullOrEmpty(end) && values.ContainsKey("timestamp"))
                match &= string.Compare(values["timestamp"]?.ToString(), end) <= 0;

            return match;
        });
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
