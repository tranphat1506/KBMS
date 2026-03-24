using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KBMS.Models;
using KBMS.Storage.V3;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Storage.V3;

/// <summary>
/// V3 User Catalog: stores User records as binary tuples in the system KB pages.
/// Replaces Engine.cs methods: CreateUser, LoadUsers, AuthenticateUser, GrantPrivilege, RevokePrivilege, DropUser.
///
/// Password is stored as a SHA-256 hash (salted with user ID).
/// All users live in the "system:users" concept page chain.
/// </summary>
public class UserCatalog
{
    private readonly StoragePool _storagePool;
    private readonly List<int> _pageIds = new();
    private readonly object _lock = new();

    public UserCatalog(StoragePool storagePool)
    {
        _storagePool = storagePool;
        LoadPageIds();
    }

    // ===================== CREATE =====================

    public User? CreateUser(string username, string rawPassword, UserRole role = UserRole.USER)
    {
        if (FindUser(username) != null)
            return null; // Already exists

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = username,
            PasswordHash = HashPassword(rawPassword, userId),
            Role = role,
            SystemAdmin = role == UserRole.ROOT,
            KbPrivileges = new Dictionary<string, Privilege>()
        };

        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;
        var diskManager = managers.Disk;

        var data = SerializeUser(user);
        lock (_lock)
        {
            var pageId = GetOrAllocatePage();
            var page = bpm.FetchPage(pageId);
            if (page == null) return null;

            var sp = new SlottedPage(page);
            if (sp.TupleCount == 0 && sp.FreeSpacePointer == 0) sp.Init(page.PageId);
            var slotId = sp.InsertTuple(data);

            if (slotId < 0)
            {
                bpm.UnpinPage(page.PageId, false);
                var newPageId = diskManager.AllocatePage();
                _pageIds.Add(newPageId);
                SavePageIds(); // Persist

                page = bpm.FetchPage(newPageId);
                if (page == null) return null;
                sp = new SlottedPage(page);
                sp.Init(newPageId);
                sp.InsertTuple(data);
            }

            bpm.UnpinPage(page.PageId, true);
        }

        return user;
    }

    // ===================== AUTHENTICATE =====================

    public User? AuthenticateUser(string username, string rawPassword)
    {
        var user = FindUser(username);
        if (user == null) return null;

        var hash = HashPassword(rawPassword, user.Id);
        return hash == user.PasswordHash ? user : null;
    }

    // ===================== READ =====================

    public User? FindUser(string username)
    {
        return ListUsers()
            .FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public List<User> ListUsers()
    {
        var results = new List<User>();
        List<int> pageSnapshot;
        lock (_lock) { pageSnapshot = new List<int>(_pageIds); }

        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;

        foreach (var pageId in pageSnapshot)
        {
            var page = bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null || raw.Length == 0) continue;
                var user = DeserializeUser(raw);
                if (user != null) results.Add(user);
            }

            bpm.UnpinPage(page.PageId, false);
        }

        return results;
    }

    // ===================== GRANT / REVOKE =====================

    public bool GrantPrivilege(string username, string kbName, Privilege privilege)
    {
        var user = FindUser(username);
        if (user == null) return false;

        user.KbPrivileges[kbName] = privilege;
        return UpdateUser(user);
    }

    public bool RevokePrivilege(string username, string kbName)
    {
        var user = FindUser(username);
        if (user == null) return false;

        user.KbPrivileges.Remove(kbName);
        return UpdateUser(user);
    }

    // ===================== DROP =====================

    public bool DropUser(string username)
    {
        List<int> pageSnapshot;
        lock (_lock) { pageSnapshot = new List<int>(_pageIds); }

        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;

        foreach (var pageId in pageSnapshot)
        {
            var page = bpm.FetchPage(pageId);
            if (page == null) continue;

            var sp = new SlottedPage(page);
            for (int i = 0; i < sp.TupleCount; i++)
            {
                var raw = sp.GetTuple(i);
                if (raw == null) continue;
                var user = DeserializeUser(raw);
                if (user != null && user.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    sp.DeleteTuple(i);
                    bpm.UnpinPage(page.PageId, true);
                    return true;
                }
            }

            bpm.UnpinPage(page.PageId, false);
        }

        return false;
    }

    // ===================== UPDATE =====================

    public bool UpdateUser(User updatedUser)
    {
        if (!DropUser(updatedUser.Username)) return false;

        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;

        var data = SerializeUser(updatedUser);
        lock (_lock)
        {
            var pageId = GetOrAllocatePage();
            var page = bpm.FetchPage(pageId);
            if (page == null) return false;
            var sp = new SlottedPage(page);
            var slotId = sp.InsertTuple(data);
            
            if (slotId < 0)
            {
                bpm.UnpinPage(page.PageId, false);
                var newPageId = _storagePool.GetManagers("system").Disk.AllocatePage();
                _pageIds.Add(newPageId);
                SavePageIds();
                
                page = bpm.FetchPage(newPageId);
                if (page == null) return false;
                sp = new SlottedPage(page);
                sp.Init(newPageId);
                sp.InsertTuple(data);
            }
            
            bpm.UnpinPage(page.PageId, true);
        }

        return true;
    }

    public bool ChangePassword(string username, string newRawPassword)
    {
        var user = FindUser(username);
        if (user == null) return false;
        user.PasswordHash = HashPassword(newRawPassword, user.Id);
        return UpdateUser(user);
    }

    // ===================== PASSWORD HASHING =====================

    private static string HashPassword(string rawPassword, Guid userId)
    {
        var input = $"{rawPassword}:{userId}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    // ===================== SERIALIZATION =====================

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private byte[] SerializeUser(User user)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(user, _jsonOptions));

    private User? DeserializeUser(byte[] data)
    {
        try { return JsonSerializer.Deserialize<User>(Encoding.UTF8.GetString(data), _jsonOptions); }
        catch { return null; }
    }

    // ===================== PERSISTENCE =====================

    private const int HEADER_OFFSET = 512;

    private void LoadPageIds()
    {
        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;
        var page = bpm.FetchPage(0);
        if (page == null) return;

        try
        {
            int count = BitConverter.ToInt32(page.Data, HEADER_OFFSET);
            if (count > 0 && count < 1000)
            {
                for (int i = 0; i < count; i++)
                {
                    int id = BitConverter.ToInt32(page.Data, HEADER_OFFSET + 4 + (i * 4));
                    if (id > 0) _pageIds.Add(id);
                }
            }
        }
        catch { }
        finally { bpm.UnpinPage(0, false); }
    }

    private void SavePageIds()
    {
        var managers = _storagePool.GetManagers("system");
        var bpm = managers.Bpm;
        var page = bpm.FetchPage(0);
        if (page == null) return;

        try
        {
            BitConverter.GetBytes(_pageIds.Count).CopyTo(page.Data, HEADER_OFFSET);
            for (int i = 0; i < _pageIds.Count; i++)
            {
                BitConverter.GetBytes(_pageIds[i]).CopyTo(page.Data, HEADER_OFFSET + 4 + (i * 4));
            }
            bpm.UnpinPage(0, true);
            bpm.FlushPage(0);
        }
        catch { bpm.UnpinPage(0, false); }
    }

    // ===================== HELPERS =====================

    private int GetOrAllocatePage()
    {
        var managers = _storagePool.GetManagers("system");
        var diskManager = managers.Disk;

        if (_pageIds.Count == 0)
        {
            var id = diskManager.AllocatePage();
            _pageIds.Add(id);
            SavePageIds();
            return id;
        }
        return _pageIds[^1];
    }
}
