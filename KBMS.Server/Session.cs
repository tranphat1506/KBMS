using System.Net.Sockets;
using KBMS.Models;

namespace KBMS.Server;

public class Session
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public User? User { get; set; }
    public string? CurrentKb { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public TcpClient? Client { get; set; }
    
    /// <summary>
    /// Ensures that only one thread can write to the client's stream at a time.
    /// This prevents message interleaving during concurrent broadcasts.
    /// </summary>
    public SemaphoreSlim MessageLock { get; } = new SemaphoreSlim(1, 1);
}
