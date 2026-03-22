using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using KBMS.CLI;
using KBMS.Network;
using KBMS.Models;
using KBMS.Server;
using KBMS.Storage;
using KBMS.Knowledge;

namespace KBMS.Tests;

public class MinimalTest
{
    public static async Task Main()
    {
        var _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_test_{Guid.NewGuid():N}");
        var storage = new StorageEngine(_testDataDir, "test_encryption_key");
        var _server = new KbmsServer("localhost", 33005, storage);
        _ = _server.StartAsync();
        
        await Task.Delay(1000);
        
        var _cli = new Cli("localhost", 33005);
        await _cli.ConnectAsync(false);
        try {
            var r1 = await _cli.ExecuteCommandAsync("LOGIN root root;");
            Console.WriteLine("LOGIN: " + r1.Content);
            var r2 = await _cli.ExecuteCommandAsync("SHOW USERS;");
            Console.WriteLine("SHOW USERS: " + r2.Content);
            
            var r3 = await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE hierarchy_test_kb;");
            var r4 = await _cli.ExecuteCommandAsync("USE hierarchy_test_kb;");
            var r5 = await _cli.ExecuteCommandAsync("CREATE CONCEPT Animal VARIABLES (name STRING);");
            var r6 = await _cli.ExecuteCommandAsync("CREATE CONCEPT Dog VARIABLES (name STRING, breed STRING);");
            var r7 = await _cli.ExecuteCommandAsync("ADD HIERARCHY Dog IS_A Animal;");
            Console.WriteLine("ADD HIERARCHY: " + r7.Content);
        } catch(Exception ex) {
            Console.WriteLine(ex.Message);
        }
        
        await _cli.DisconnectAsync();
        _server.Stop();
    }
}
