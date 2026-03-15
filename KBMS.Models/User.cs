namespace KBMS.Models;

public enum UserRole
{
    ROOT,
    USER
}

public enum Privilege
{
    READ,
    WRITE,
    ADMIN
}

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Dictionary<string, Privilege> KbPrivileges { get; set; } = new();
    public bool SystemAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
}
