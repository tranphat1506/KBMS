using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using KBMS.CLI;
using KBMS.Server;

namespace KBMS.Tests
{
    [Collection("SequentialServerTests")]
    public class LoadAndStressTests : IAsyncDisposable
    {
        private KbmsServer _server;
        private string _dataDir;
        private int _port;
        private static int _nextPort = 39000;

        public LoadAndStressTests()
        {
            _dataDir = Path.Combine(Path.GetTempPath(), "kbms_load_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _port = Interlocked.Increment(ref _nextPort);
        }

        private async Task StartServerAsync()
        {
            _server = new KbmsServer("127.0.0.1", _port, _dataDir);
            _ = _server.StartAsync();
            await Task.Delay(500); // Wait for boot
        }

        public async ValueTask DisposeAsync()
        {
            _server?.Stop();
            if (Directory.Exists(_dataDir)) 
            {
                try { Directory.Delete(_dataDir, true); } catch {}
            }
        }

        [Fact]
        public async Task Test_256_Concurrent_Connections()
        {
            await StartServerAsync();
            int connectionCount = 256;
            var tasks = new List<Task<bool>>();

            for (int i = 0; i < connectionCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var cli = new Cli("127.0.0.1", _port);
                        await cli.ConnectAsync();
                        var res = await cli.ExecuteCommandAsync("LOGIN root root");
                        await cli.DisconnectAsync();
                        return res?.Content.Contains("LOGIN_SUCCESS") ?? false;
                    }
                    catch { return false; }
                }));
            }

            var results = await Task.WhenAll(tasks);
            int successes = 0;
            foreach (var r in results) if (r) successes++;

            Assert.True(successes > 200, $"Expected at least 200 successful connections, got {successes}.");
        }

        [Fact]
        public async Task Test_Bulk_Volume_Insert_Performance()
        {
            await StartServerAsync();
            var cli = new Cli("127.0.0.1", _port);
            await cli.ConnectAsync();
            await cli.ExecuteCommandAsync("LOGIN root root");
            await cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE VolumeKB;");
            await cli.ExecuteCommandAsync("USE VolumeKB;");
            await cli.ExecuteCommandAsync("CREATE CONCEPT Data (id INT, val STRING);");

            // Chèn 1000 bản ghi để kiểm tra hiệu năng cơ bản (trong môi trường test)
            int count = 1000;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < count; i++)
            {
                await cli.ExecuteCommandAsync($"INSERT INTO Data ATTRIBUTE ({i}, 'Value_{i}');");
            }
            
            watch.Stop();
            // Đảm bảo dữ liệu đã được ghi và có thể truy vấn lại
            var selectRes = await cli.ExecuteCommandAsync("SELECT * FROM Data WHERE id = 999;");
            Assert.Contains("Value_999", selectRes?.Content);
            
            await cli.DisconnectAsync();
        }
    }
}
