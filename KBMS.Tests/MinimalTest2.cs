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

public class MinimalTest2
{
    public static async Task Main()
    {
        var _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_test_{Guid.NewGuid():N}");
        var storage = new StorageEngine(_testDataDir, "test_encryption_key");
        var _server = new KbmsServer("localhost", 33008, storage);
        _ = _server.StartAsync();
        
        await Task.Delay(1000);
        
        var _cli = new Cli("localhost", 33008);
        await _cli.ConnectAsync(false);
        try {
            await _cli.ExecuteCommandAsync("LOGIN root root;");
            
            // Delete Tests
            await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE delete_test_kb;");
            await _cli.ExecuteCommandAsync("USE delete_test_kb;");
            await _cli.ExecuteCommandAsync("CREATE CONCEPT Temp VARIABLES (id INT, value STRING);");
            var insert1 = await _cli.ExecuteCommandAsync("INSERT INTO Temp VALUES (1, 'to delete');");
            Console.WriteLine("INSERT: " + insert1.Content);
            
            var del1 = await _cli.ExecuteCommandAsync("DELETE FROM Temp WHERE id = 1;");
            Console.WriteLine("DELETE ERROR: " + del1.Content);
            
            // TC022 Hierarchy
            var creH = await _cli.ExecuteCommandAsync("CREATE HIERARCHY Dog ISA Animal;");
            Console.WriteLine("HIERARCHY ERROR: " + creH.Content);
        } catch(Exception ex) {
            Console.WriteLine(ex.Message);
        }
        
        await _cli.DisconnectAsync();
        _server.Stop();
    }
}
