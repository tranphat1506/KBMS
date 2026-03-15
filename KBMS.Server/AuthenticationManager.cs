using System.Linq;
using KBMS.Models;
using KBMS.Storage;

namespace KBMS.Server;

public class AuthenticationManager
{
    private readonly StorageEngine _storage;

    public AuthenticationManager(StorageEngine storage)
    {
        _storage = storage;
    }

    public User? Login(string username, string password)
    {
        var users = _storage.LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            return null;
        }

        var verified = _storage.VerifyPassword(password, user.PasswordHash);

        if (!verified)
        {
            return null;
        }

        return user;
    }

    public bool CheckPrivilege(User user, string action, string? kbName = null)
    {
        // ROOT - không cần check permission
        if (user.Role == UserRole.ROOT)
            return true;

        // USER - cần check
        return CheckUserPrivilege(user, action, kbName);
    }

    private bool CheckUserPrivilege(User user, string action, string? kbName)
    {
        switch (action.ToUpper())
        {
            case "CREATE_KB":
                return user.SystemAdmin;

            case "DROP_KB":
                if (kbName == null) return false;
                return user.KbPrivileges.TryGetValue(kbName, out var priv1) && priv1 == Privilege.ADMIN;

            case "SELECT":
            case "SOLVE":
                if (kbName == null) return false;
                return user.KbPrivileges.ContainsKey(kbName);

            case "INSERT":
            case "UPDATE":
            case "DELETE":
                if (kbName == null) return false;
                return user.KbPrivileges.TryGetValue(kbName, out var priv2) && (priv2 == Privilege.WRITE || priv2 == Privilege.ADMIN);

            case "CREATE_CONCEPT":
            case "CREATE_RULE":
            case "CREATE_OPERATOR":
            case "CREATE_FUNCTION":
                if (kbName == null) return false;
                return user.KbPrivileges.TryGetValue(kbName, out var priv3) && priv3 == Privilege.ADMIN;

            case "GRANT":
                return user.SystemAdmin;

            default:
                return false;
        }
    }

    public User? CreateUser(string username, string password, UserRole role, bool systemAdmin = false)
    {
        return _storage.CreateUser(username, password, role, systemAdmin);
    }

    public bool GrantPrivilege(string username, string kbName, Privilege privilege)
    {
        var users = _storage.LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null || user.Role == UserRole.ROOT)
            return false;

        user.KbPrivileges[kbName] = privilege;
        _storage.SaveUsers(users);

        return true;
    }
}
