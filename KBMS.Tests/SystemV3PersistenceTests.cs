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
    [Fact]
    public async Task V3_Storage_Persistence_AcrossRestarts_ShouldWork()
    {
        string testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_persistence_{Guid.NewGuid():N}");
        int testPort = 35001;

        // Phase 1: Create Data
        {
            var server = new KbmsServer("localhost", testPort, testDataDir);
            var serverTask = server.StartAsync();
            await Task.Delay(500);

            var cli = new KBMS.CLI.Cli("localhost", testPort);
            await cli.ConnectAsync();
            await cli.ExecuteCommandAsync("LOGIN root root");
            
            await cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE persist_db;");
            await cli.ExecuteCommandAsync("USE persist_db;");
            await cli.ExecuteCommandAsync("CREATE CONCEPT Person (name: STRING, age: INT);");
            await cli.ExecuteCommandAsync("INSERT INTO Person ATTRIBUTE ('Le Chau', 23);");
            
            await cli.DisconnectAsync();
            server.Stop();
            await serverTask;
            await Task.Delay(500);
        }

        // Phase 2: Restart and Verify
        {
            KbmsServer? server = null;
            for (int retry = 0; retry < 5; retry++)
            {
                try
                {
                    server = new KbmsServer("localhost", testPort, testDataDir);
                    break;
                }
                catch (IOException) when (retry < 4)
                {
                    await Task.Delay(500);
                }
            }
            
            if (server == null) throw new Exception("Failed to restart server due to file locks.");
            var serverTask = server.StartAsync();
            await Task.Delay(500);

            var cli = new KBMS.CLI.Cli("localhost", testPort);
            await cli.ConnectAsync();
            await cli.ExecuteCommandAsync("LOGIN root root");
            
            await cli.ExecuteCommandAsync("USE persist_db;");
            
            var res1 = await cli.ExecuteCommandAsync("SHOW CONCEPTS;");
            Assert.Contains("Person", res1!.Content);
            
            var res2 = await cli.ExecuteCommandAsync("SELECT * FROM Person;");
            Assert.Contains("Le Chau", res2!.Content);
            Assert.Contains("23", res2.Content);
            
            await cli.DisconnectAsync();
            server.Stop();
            await serverTask;
        }

        if (Directory.Exists(testDataDir)) Directory.Delete(testDataDir, true);
    }
}
