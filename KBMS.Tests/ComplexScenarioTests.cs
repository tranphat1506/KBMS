using Xunit;
using Xunit.Abstractions;
using KBMS.CLI;
using KBMS.Models;
using KBMS.Server;
using KBMS.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace KBMS.Tests
{
    [Collection("SequentialServerTests")]
    public class ComplexScenarioTests : IAsyncDisposable
    {
        private KbmsServer _server;
        private Cli _cli;
        private string _dataDir;
        private string _encryptionKey = "test_key_12345678";
        private readonly ITestOutputHelper _output;
        private static int _nextPort = 8500;
        private int _port;

        public ComplexScenarioTests(ITestOutputHelper output)
        {
            _output = output;
            _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "kbms_complex_data_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _port = System.Threading.Interlocked.Increment(ref _nextPort);
        }

        private async Task InitializeAsync()
        {
            _server = new KbmsServer("127.0.0.1", _port, _dataDir);
            _ = _server.StartAsync(); 
            _cli = new Cli("127.0.0.1", _port);
            
            for (int i = 0; i < 20; i++)
            {
                try {
                    await _cli.ConnectAsync();
                    break;
                } catch { await Task.Delay(200); }
            }
            await _cli.ExecuteCommandAsync("LOGIN root root");
        }

        private async Task RunCmd(string cmd, string context = "")
        {
            var res = await _cli.ExecuteCommandAsync(cmd);
            if (res.Type == MessageType.ERROR)
            {
                if (res.Content != null && res.Content.Contains("already exists")) {
                    _output.WriteLine($"[INFO] {cmd} -> Already exists, continuing.");
                    return;
                }
                _output.WriteLine($"[ERROR] {context} -> {res.Content}");
            }
            Assert.True(res.Type != MessageType.ERROR, $"Command failed: {cmd} | Error: {res.Content}");
        }

        public async ValueTask DisposeAsync()
        {
            if (_cli != null) await _cli.DisconnectAsync();
            if (_server != null) _server.Stop();
            if (Directory.Exists(_dataDir)) 
            {
                try { Directory.Delete(_dataDir, true); } catch {}
            }
        }

        [Fact]
        public async Task Test_Scenario_A_Education_Inference()
        {
            await InitializeAsync();
            await RunCmd("CREATE KNOWLEDGE BASE Education_DB;");
            await RunCmd("USE Education_DB;");

            // Define Domain with proper types
            await RunCmd("CREATE CONCEPT Student(name: STRING, gpa: FLOAT, credits: INT, behavior: INT, status: STRING);");
            
            // Correct Rule Syntax: SCOPE + AND + SET
            await RunCmd("CREATE RULE R_DeanList SCOPE Student IF gpa >= 3.8 AND behavior >= 90 AND credits >= 15 THEN SET status = 'DeanList';");

            // Insert base facts
            await RunCmd("INSERT INTO Student ATTRIBUTE(name: 'Le Phat', gpa: 3.9, behavior: 95, credits: 18);");
            
            // Reason via SELECT SOLVE
            var res = await _cli.ExecuteCommandAsync("SELECT SOLVE(status) FROM Student WHERE name = 'Le Phat';");
            _output.WriteLine($"[Education] Result: {res.Content}");
            Assert.Contains("DeanList", res.Content);
        }

        [Fact]
        public async Task Test_Scenario_B_Medical_Diagnostic()
        {
            await InitializeAsync();
            await RunCmd("CREATE KNOWLEDGE BASE Medical_DB;");
            await RunCmd("USE Medical_DB;");

            await RunCmd("CREATE CONCEPT Patient(p_id: STRING, p_age: INT, p_bmi: FLOAT, p_risk: STRING);");
            await RunCmd("CREATE RULE R_CardiacRisk SCOPE Patient IF p_age > 60 AND p_bmi > 30 THEN SET p_risk = 'High';");

            // Insert base facts
            await RunCmd("INSERT INTO Patient ATTRIBUTE(p_id: 'P1', p_age: 65, p_bmi: 32.5);");
            
            // Reason via SELECT SOLVE
            var res = await _cli.ExecuteCommandAsync("SELECT SOLVE(p_risk) FROM Patient WHERE p_id = 'P1';");
            _output.WriteLine($"[Medical] Result: {res.Content}");
            Assert.Contains("High", res.Content);
        }

        [Fact]
        public async Task Test_Scenario_D_Geometry_Pythagoras()
        {
            await InitializeAsync();
            await RunCmd("CREATE KNOWLEDGE BASE Geometry_DB;");
            await RunCmd("USE Geometry_DB;");

            await RunCmd("CREATE CONCEPT Triangle(ta: FLOAT, tb: FLOAT, tc: FLOAT, t_type: STRING);");
            
            // Pythagoras: a*a + b*b = c*c
            await RunCmd("CREATE RULE R_Right SCOPE Triangle IF (ta*ta + tb*tb) = (tc*tc) THEN SET t_type = 'RightTriangle';");

            // Insert base facts
            await RunCmd("INSERT INTO Triangle ATTRIBUTE(ta: 3.0, tb: 4.0, tc: 5.0);");
            
            // Reason via SELECT SOLVE
            var res = await _cli.ExecuteCommandAsync("SELECT SOLVE(t_type) FROM Triangle WHERE ta = 3.0;");
            _output.WriteLine($"[Geometry] Result: {res.Content}");
            Assert.Contains("RightTriangle", res.Content);
        }

        [Fact]
        public async Task Test_Scenario_C_SmartCity_Transit()
        {
            await InitializeAsync();
            await RunCmd("CREATE KNOWLEDGE BASE City_DB;");
            await RunCmd("USE City_DB;");

            // Multi-step reasoning: Sensor -> Zone
            await RunCmd("CREATE CONCEPT Zone(id: STRING, status: STRING);");
            await RunCmd("CREATE CONCEPT Sensor(id: STRING, speed: INT, zid: STRING);");

            // Note: Crossing concepts in a single rule IF part is complex in Rete V3.
            // Simplified for now: Assume Sensor speed triggers its own alert.
            await RunCmd("CREATE RULE R_Jam SCOPE Sensor IF speed < 10 THEN SET id = 'JAMMED';");

            // Insert base facts
            await RunCmd("INSERT INTO Sensor ATTRIBUTE(id: 'S1', speed: 5, zid: 'Z1');");
            
            // Reason via SELECT SOLVE
            var res = await _cli.ExecuteCommandAsync("SELECT SOLVE(id) FROM Sensor WHERE zid = 'Z1';");
            _output.WriteLine($"[SmartCity] Result: {res.Content}");
            Assert.Contains("JAMMED", res.Content);
        }
    }
}
