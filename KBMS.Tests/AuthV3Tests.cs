using System;
using System.IO;
using System.Linq;
using Xunit;
using KBMS.Storage.V3;
using KBMS.Models;

namespace KBMS.Tests;

/// <summary>
/// Layer 5: User / Auth V3 Tests
/// Verifies that UserCatalog correctly stores, authenticates, and manages users in V3 binary pages.
/// </summary>
public class AuthV3Tests : IDisposable
{
    private readonly string _dbPath;
    private readonly DiskManager _disk;
    private readonly BufferPoolManager _bpm;
    private readonly UserCatalog _users;

    public AuthV3Tests()
    {
        _dbPath = Path.GetTempFileName() + ".kdb";
        _disk = new DiskManager(_dbPath);
        _bpm = new BufferPoolManager(_disk, 32);
        _users = new UserCatalog(_bpm, _disk);
    }

    public void Dispose()
    {
        _bpm?.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public void CreateUser_Persists_And_Loadable()
    {
        var user = _users.CreateUser("alice", "Secret123!", UserRole.ROOT);
        Assert.NotNull(user);
        Assert.Equal("alice", user!.Username);

        var found = _users.FindUser("alice");
        Assert.NotNull(found);
        Assert.Equal(UserRole.ROOT, found!.Role);
    }

    [Fact]
    public void AuthenticateUser_CorrectPassword_Succeeds()
    {
        _users.CreateUser("bob", "p@ss!");
        var authenticated = _users.AuthenticateUser("bob", "p@ss!");
        Assert.NotNull(authenticated);
        Assert.Equal("bob", authenticated!.Username);
    }

    [Fact]
    public void AuthenticateUser_WrongPassword_Fails()
    {
        _users.CreateUser("charlie", "correct");
        var result = _users.AuthenticateUser("charlie", "wrong");
        Assert.Null(result);
    }

    [Fact]
    public void GrantPrivilege_UpdatesUserAccess()
    {
        _users.CreateUser("dave", "pwd");
        _users.GrantPrivilege("dave", "SchoolDB", Privilege.WRITE);

        var user = _users.FindUser("dave");
        Assert.NotNull(user);
        Assert.True(user!.KbPrivileges.ContainsKey("SchoolDB"));
        Assert.Equal(Privilege.WRITE, user.KbPrivileges["SchoolDB"]);
    }

    [Fact]
    public void RevokePrivilege_RemovesAccess()
    {
        _users.CreateUser("eve", "pwd");
        _users.GrantPrivilege("eve", "MedicalDB", Privilege.ADMIN);
        _users.RevokePrivilege("eve", "MedicalDB");

        var user = _users.FindUser("eve");
        Assert.NotNull(user);
        Assert.False(user!.KbPrivileges.ContainsKey("MedicalDB"));
    }

    [Fact]
    public void DropUser_RemovesFromCatalog()
    {
        _users.CreateUser("ghost", "boo");
        Assert.NotNull(_users.FindUser("ghost"));

        _users.DropUser("ghost");
        Assert.Null(_users.FindUser("ghost"));
        Assert.Empty(_users.ListUsers());
    }
}
