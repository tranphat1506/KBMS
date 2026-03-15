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
}
