using KBMS.Server;
using KBMS.Network;
using KBMS.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace KBMS.Tests;

public class SystemV3PersistenceTests
{
    public async Task V3_Storage_Persistence_AcrossRestarts_ShouldWork()
    {
        string testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_persistence_{Guid.NewGuid():N}");
        int testPort = 35000 + (new Random().Next(5000));

        // Phase 1: Create Data
        {
            var server = new KbmsServer("localhost", testPort, testDataDir);
            var serverTask = server.StartAsync();
            await Task.Delay(1000);

            var cli = new KBMS.CLI.Cli("localhost", testPort);
            await cli.ConnectAsync();
            await cli.ExecuteCommandAsync("LOGIN root root");
            
            await cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE persist_db;");
            await cli.ExecuteCommandAsync("USE persist_db;");
            await cli.ExecuteCommandAsync("CREATE CONCEPT Person (id: INT, name: STRING, age: INT);");
            await cli.ExecuteCommandAsync("INSERT INTO Person ATTRIBUTE (1, 'Le Chau', 23);");
            
            await cli.DisconnectAsync();
            server.Stop();
            await serverTask;
            await Task.Delay(3000); // 3 seconds to ensure disk flush & socket release
        }

        // Phase 2: Restart and Verify
        {
            var server = new KbmsServer("localhost", testPort, testDataDir);
            var serverTask = server.StartAsync();
            await Task.Delay(1500); // Wait for port and files to be ready

            var cli = new KBMS.CLI.Cli("localhost", testPort);
            await cli.ConnectAsync();
            await cli.ExecuteCommandAsync("LOGIN root root");
            
            await cli.ExecuteCommandAsync("USE persist_db;");
            
            var res1 = await cli.ExecuteCommandAsync("SHOW CONCEPTS;");
            Assert.Contains("Person", res1!.Content);
            
            // WHERE clause with alias prefix 'p.'
            var res = await cli.ExecuteCommandAsync("SELECT p.id FROM Product p WHERE p.price > 500;");
            // Use flexible matching for JSON (ignore spaces/quotes slightly if needed)
            Assert.Matches(@"\[\{""id"":\s*1(\.0)?\}\]", res!.Content);
            Assert.DoesNotContain(@"{""id"":2}", res.Content);
            
            await cli.DisconnectAsync();
            server.Stop();
            await serverTask;
            await Task.Delay(1000);
        }

        if (Directory.Exists(testDataDir)) Directory.Delete(testDataDir, true);
    }
}
