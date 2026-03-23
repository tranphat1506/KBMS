using Xunit;
using KBMS.CLI;
using KBMS.Models;
using KBMS.Server;
using KBMS.Storage;
using KBMS.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace KBMS.Tests
{
    [Collection("SequentialServerTests")]
    public class Phase5ForwardChainingTests : IAsyncDisposable
    {
        private KbmsServer _server;
        private Cli _cli;
        private string _dataDir;
        private string _encryptionKey = "test_key_12345678";
        private static int _nextPort = 8400;
        private int _port;

        public Phase5ForwardChainingTests()
        {
            _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "kbms_tests_data_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _port = System.Threading.Interlocked.Increment(ref _nextPort);
        }

        private async Task InitializeAsync()
        {
            var storage = new StorageEngine(_dataDir, _encryptionKey);
            _server = new KbmsServer("127.0.0.1", _port, storage);
            _ = _server.StartAsync(); 
            _cli = new Cli("127.0.0.1", _port);
            
            bool connected = false;
            for (int i = 0; i < 15; i++)
            {
                try {
                    await _cli.ConnectAsync();
                    connected = true;
                    break;
                } catch { await Task.Delay(200); }
            }
            Assert.True(connected, "Failed to connect to test server.");

            var loginRes = await _cli.ExecuteCommandAsync("LOGIN root root");
            Assert.True(loginRes?.Type != MessageType.ERROR, $"Login failed: {loginRes?.Content}");
        }

        private async Task CleanupAsync()
        {
            if (_cli != null) await _cli.DisconnectAsync();
            if (_server != null) _server.Stop();
            await Task.Delay(500); 
            if (Directory.Exists(_dataDir)) 
            {
                try { Directory.Delete(_dataDir, true); } catch {}
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CleanupAsync();
        }

        [Fact]
        public async Task Insert_ShouldTriggerForwardChaining()
        {
            await InitializeAsync();
            var dbName = "db_ins_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            
            var res = await _cli.ExecuteCommandAsync($"CREATE KNOWLEDGE BASE {dbName};");
            Assert.True(res?.Type != MessageType.ERROR, $"Failed to create KB: {res?.Content}");

            var useRes = await _cli.ExecuteCommandAsync($"USE {dbName};");
            Assert.True(useRes?.Type != MessageType.ERROR, $"Failed to USE KB: {useRes?.Content}");

            var conceptRes = await _cli.ExecuteCommandAsync("CREATE CONCEPT Student(name STRING, grade FLOAT, honor STRING);");
            Assert.True(conceptRes!.Type != MessageType.ERROR, $"Failed to create concept: {conceptRes.Content}");

            var ruleRes = await _cli.ExecuteCommandAsync("CREATE RULE HighHonor IF Student(grade >= 90) THEN Student(honor = 'High');");
            Assert.True(ruleRes!.Type != MessageType.ERROR, $"Failed to create rule: {ruleRes.Content}");

            var insRes = await _cli.ExecuteCommandAsync("INSERT INTO Student ATTRIBUTE (name: 'Alice', grade: 95);");
            Assert.True(insRes!.Type != MessageType.ERROR, $"Failed to insert: {insRes.Content}");

            var selectRes = await _cli.ExecuteCommandAsync("SELECT honor FROM Student WHERE name = 'Alice';");
            Assert.Contains("High", selectRes!.Content);
        }

        [Fact]
        public async Task Update_ShouldTriggerForwardChaining()
        {
            await InitializeAsync();
            var dbName = "db_upd_" + Guid.NewGuid().ToString("N").Substring(0, 6);

            var res = await _cli.ExecuteCommandAsync($"CREATE KNOWLEDGE BASE {dbName};");
            Assert.True(res?.Type != MessageType.ERROR, $"Failed to create KB: {res?.Content}");

            await _cli.ExecuteCommandAsync($"USE {dbName};");
            await _cli.ExecuteCommandAsync("CREATE CONCEPT Student(name STRING, grade FLOAT, honor STRING);");
            await _cli.ExecuteCommandAsync("CREATE RULE HighHonor IF Student(grade >= 90) THEN Student(honor = 'High');");
            
            await _cli.ExecuteCommandAsync("INSERT INTO Student ATTRIBUTE (name: 'Bob', grade: 60);");
            var select1 = await _cli.ExecuteCommandAsync("SELECT honor FROM Student WHERE name = 'Bob';");
            Assert.DoesNotContain("High", select1!.Content);

            var updRes = await _cli.ExecuteCommandAsync("UPDATE Student ATTRIBUTE (SET grade: 91) WHERE name = 'Bob';");
            Assert.True(updRes!.Type != MessageType.ERROR, $"Failed to update: {updRes.Content}");

            var select2 = await _cli.ExecuteCommandAsync("SELECT honor FROM Student WHERE name = 'Bob';");
            Assert.Contains("High", select2!.Content);
        }

        [Fact]
        public async Task RecursiveRules_ShouldTriggerMultipleSteps()
        {
            await InitializeAsync();
            var dbName = "db_rec_" + Guid.NewGuid().ToString("N").Substring(0, 6);

            var res = await _cli.ExecuteCommandAsync($"CREATE KNOWLEDGE BASE {dbName};");
            Assert.True(res?.Type != MessageType.ERROR, $"Failed to create KB: {res?.Content}");

            await _cli.ExecuteCommandAsync($"USE {dbName};");
            await _cli.ExecuteCommandAsync("CREATE CONCEPT Student(name STRING, grade FLOAT, honor STRING, gifted BOOL);");
            
            await _cli.ExecuteCommandAsync("CREATE RULE HighHonor IF Student(grade >= 90) THEN Student(honor = 'High');");
            await _cli.ExecuteCommandAsync("CREATE RULE HonorToGift IF Student(honor = 'High') THEN Student(gifted = true);");

            await _cli.ExecuteCommandAsync("INSERT INTO Student ATTRIBUTE (name: 'Charlie', grade: 95);");
            
            var selectRes = await _cli.ExecuteCommandAsync("SELECT honor, gifted FROM Student WHERE name = 'Charlie';");
            Assert.Contains("High", selectRes!.Content);
            Assert.Contains("true", selectRes.Content);
        }
    }

    [CollectionDefinition("SequentialServerTests", DisableParallelization = true)]
    public class SequentialServerTestsCollection { }
}
