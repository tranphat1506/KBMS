namespace KBMS.Network;

public class Message
{
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? RequestId { get; set; }
}
