using System;
using System.Collections.Concurrent;
using KBMS.Models;

namespace KBMS.Server;

public class ConnectionManager
{
    private readonly ConcurrentDictionary<string, Session> _sessions;
    private readonly Random _random = new();

    public ConnectionManager()
    {
        _sessions = new ConcurrentDictionary<string, Session>();
    }

    public Session CreateSession(string clientId)
    {
        var sessionId = GenerateSessionId();
        var session = new Session
        {
            SessionId = sessionId,
            ClientId = clientId,
            User = null,
            CurrentKb = null,
            ConnectedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _sessions[clientId] = session;
        return session;
    }

    public Session? GetSession(string clientId)
    {
        return _sessions.TryGetValue(clientId, out var session) ? session : null;
    }

    public Session? GetSessionBySessionId(string sessionId)
    {
        foreach (var session in _sessions.Values)
        {
            if (session.SessionId == sessionId)
                return session;
        }
        return null;
    }

    public void SetSessionUser(string clientId, User? user)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.User = user;
            session.LastActivityAt = DateTime.UtcNow;
        }
    }

    public void SetSessionKb(string clientId, string? kbName)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.CurrentKb = kbName;
            session.LastActivityAt = DateTime.UtcNow;
        }
    }

    public void UpdateActivity(string clientId)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.LastActivityAt = DateTime.UtcNow;
        }
    }

    public void RemoveSession(string clientId)
    {
        _sessions.TryRemove(clientId, out _);
    }

    public bool IsAuthenticated(string clientId)
    {
        var session = GetSession(clientId);
        return session?.User != null;
    }

    public User? GetCurrentUser(string clientId)
    {
        return GetSession(clientId)?.User;
    }

    public string? GetCurrentKb(string clientId)
    {
        return GetSession(clientId)?.CurrentKb;
    }

    public void CleanupExpiredSessions(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = new List<string>();

        foreach (var kvp in _sessions)
        {
            if (now - kvp.Value.LastActivityAt > timeout)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _sessions.TryRemove(key, out _);
        }
    }

    private string GenerateSessionId()
    {
        var bytes = new byte[16];
        _random.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }
}
