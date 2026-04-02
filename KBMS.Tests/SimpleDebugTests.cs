using Xunit;
using Xunit.Abstractions;
using KBMS.CLI;
using KBMS.Server;
using KBMS.Network;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KBMS.Tests
{
    [Collection("SequentialServerTests")]
    public class SimpleDebugTests : IAsyncDisposable
    {
        private KbmsServer _server;
        private Cli _cli;
        private string _dataDir;
        private readonly ITestOutputHelper _output;
        private int _port = 8600;

        public SimpleDebugTests(ITestOutputHelper output)
        {
            _output = output;
            _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "kbms_debug_data_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
        }

        private async Task Init()
        {
            _server = new KbmsServer("127.0.0.1", _port, _dataDir);
            _ = _server.StartAsync(); 
            _cli = new Cli("127.0.0.1", _port);
            for (int i = 0; i < 15; i++) { try { await _cli.ConnectAsync(); break; } catch { await Task.Delay(200); } }
            await _cli.ExecuteCommandAsync("LOGIN root root");
        }

        public async ValueTask DisposeAsync()
        {
            if (_cli != null) await _cli.DisconnectAsync();
            if (_server != null) _server.Stop();
            await Task.Delay(500); 
            if (Directory.Exists(_dataDir)) 
            {
                try { Directory.Delete(_dataDir, true); } catch {}
            }
        }

        [Fact]
        public async Task Debug_Insert_Select_Roundtrip()
        {
            await Init();
            
            var r1 = await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE DebugDB;");
            _output.WriteLine($"CREATE KB: {r1.Content}");

            var r2 = await _cli.ExecuteCommandAsync("USE DebugDB;");
            _output.WriteLine($"USE KB: {r2.Content}");

            var r3 = await _cli.ExecuteCommandAsync("CREATE CONCEPT Item(name: STRING, val: INT);");
            _output.WriteLine($"CREATE CONCEPT: {r3.Content}");

            var r4 = await _cli.ExecuteCommandAsync("INSERT INTO Item ATTRIBUTE (name: 'TestObj', val: 42);");
            _output.WriteLine($"INSERT: {r4.Content}");

            var r5 = await _cli.ExecuteCommandAsync("SELECT * FROM Item;");
            _output.WriteLine($"SELECT ALL: {r5.Content}");

            Assert.Contains("TestObj", r5.Content);
        }
    }
}
