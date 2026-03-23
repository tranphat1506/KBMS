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
using KBMS.Models.V3;

namespace KBMS.Server.V3;

public class ManagementManager
{
    private readonly ConnectionManager _connectionManager;
    private readonly SystemLogger _sysLogger;
    private readonly ConcurrentDictionary<string, byte> _logSubscribers = new();
    private readonly Process _currentProcess;

    public ManagementManager(ConnectionManager connectionManager, SystemLogger sysLogger)
    {
        _connectionManager = connectionManager;
        _sysLogger = sysLogger;
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
}
