using System;

namespace KBMS.Server;

/// <summary>
/// Log levels for the KBMS server
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// Structured logger for KBMS Server with timestamps, levels, and session tracking
/// </summary>
public class Logger
{
    private readonly LogLevel _minLevel;
    private KBMS.Server.V3.SystemLogger? _sysLogger;

    public Logger(LogLevel minLevel = LogLevel.Info)
    {
        _minLevel = minLevel;
    }

    public void SetSystemLogger(KBMS.Server.V3.SystemLogger sysLogger)
    {
        _sysLogger = sysLogger;
    }

    /// <summary>
    /// Base logging method with timestamp and level
    /// </summary>
    public void Log(LogLevel level, string sessionId, string message)
    {
        if (level < _minLevel)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        Console.WriteLine($"[{timestamp}] [{levelStr}] [{sessionId}] {message}");

        // Mirror to System KB if available
        _sysLogger?.LogSystemEvent(level.ToString(), $"[{sessionId}] {message}");
    }

    /// <summary>
    /// Log an incoming request with username
    /// </summary>
    public void LogRequest(string sessionId, string command, string? username = null)
    {
        var userPrefix = username != null ? $"[{username}] " : "";
        Log(LogLevel.Info, sessionId, $"{userPrefix}REQUEST: {command}");
    }

    /// <summary>
    /// Log a response with type and content length
    /// </summary>
    public void LogResponse(string sessionId, string type, int contentLength)
    {
        Log(LogLevel.Info, sessionId, $"RESPONSE: {type} ({contentLength} bytes)");
    }

    public void Debug(string sessionId, string message) => Log(LogLevel.Debug, sessionId, message);
    public void Info(string sessionId, string message) => Log(LogLevel.Info, sessionId, message);
    public void Warning(string sessionId, string message) => Log(LogLevel.Warning, sessionId, message);

    public void Error(string sessionId, string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message} - {ex.Message}" : message;
        Log(LogLevel.Error, sessionId, fullMessage);
    }
}
