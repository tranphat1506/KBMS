using System;
using System.Collections.Generic;

namespace KBMS.Models.V3;

public class SystemStats
{
    public double CpuUsage { get; set; }
    public long MemoryMb { get; set; }
    public string Uptime { get; set; } = string.Empty;
    public int ActiveSessions { get; set; }
    public string EngineVersion { get; set; } = "3.1.0-beta";
}

public class SessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? CurrentKb { get; set; }
}
