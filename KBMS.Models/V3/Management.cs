using System.Text.Json.Serialization;
using KBMS.Models;

namespace KBMS.Models.V3;

public class ManagementCommandPayload
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public UserRole Role { get; set; } = UserRole.USER;

    [JsonPropertyName("kb")]
    public string Kb { get; set; } = string.Empty;

    [JsonPropertyName("privilege")]
    public Privilege Privilege { get; set; } = Privilege.READ;

    [JsonPropertyName("logType")]
    public string LogType { get; set; } = "system";

    [JsonPropertyName("userFilter")]
    public string UserFilter { get; set; } = string.Empty;

    [JsonPropertyName("logLevel")]
    public string LogLevel { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("settingName")]
    public string SettingName { get; set; } = string.Empty;

    [JsonPropertyName("settingValue")]
    public string SettingValue { get; set; } = string.Empty;
    
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
}
