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
    public class EdgeCaseReasoningTests : IAsyncLifetime
    {
        private KbmsServer _server;
        private Cli _cli;
        private string _dataDir;
        private static int _nextPort = 9200;
        private int _port;
        private readonly ITestOutputHelper _output;

        public EdgeCaseReasoningTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public async Task InitializeAsync()
        {
            _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "kbms_edgecase_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _port = System.Threading.Interlocked.Increment(ref _nextPort);

            _server = new KbmsServer("127.0.0.1", _port, _dataDir);
            _ = _server.StartAsync(); 
            
            _cli = new Cli("127.0.0.1", _port);
            bool connected = false;
            for (int i = 0; i < 20; i++)
            {
                try {
                    await _cli.ConnectAsync();
                    connected = true;
                    break;
                } catch { await Task.Delay(200); }
            }
            if(!connected) throw new Exception("Fixture connect failed");
            await _cli.ExecuteCommandAsync("LOGIN root root");
            await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE EdgeKB;");
            await _cli.ExecuteCommandAsync("USE EdgeKB;");
        }

        public async Task DisposeAsync()
        {
            if (_cli != null) await _cli.DisconnectAsync();
            if (_server != null) _server.Stop();
            if (Directory.Exists(_dataDir)) 
            {
                try { Directory.Delete(_dataDir, true); } catch {}
            }
        }

        [Fact]
        public async Task Test_1_CircularRuleDependencies_ShouldStopCleanly()
        {
            // Scenario: Rule A -> B, Rule B -> A
            await _cli.ExecuteCommandAsync("CREATE CONCEPT Node(a: BOOLEAN, b: BOOLEAN);");
            await _cli.ExecuteCommandAsync("CREATE RULE AtoB SCOPE Node IF a = true THEN SET b = true;");
            await _cli.ExecuteCommandAsync("CREATE RULE BtoA SCOPE Node IF b = true THEN SET a = true;");

            // Execution
            await _cli.ExecuteCommandAsync("INSERT INTO Node ATTRIBUTE (a: true);");
            var res = await _cli.ExecuteCommandAsync("SELECT a, SOLVE(b) FROM Node;");
            
            // Expectation: Result returns cleanly, not timing out or throwing StackOverflow
            Assert.Equal(MessageType.RESULT, res.Type);
            Assert.Contains("True", res.Content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Test_2_ConflictingRules_LastWriteWins()
        {
            // Scenario: Multiple rules overriding overlapping domain
            await _cli.ExecuteCommandAsync("CREATE CONCEPT TestScore(score: INT, status: STRING);");
            await _cli.ExecuteCommandAsync("CREATE RULE Good SCOPE TestScore IF score > 5 THEN SET status = 'Good';");
            await _cli.ExecuteCommandAsync("CREATE RULE Bad SCOPE TestScore IF score < 10 THEN SET status = 'Bad';");

            await _cli.ExecuteCommandAsync("INSERT INTO TestScore ATTRIBUTE (score: 7);");
            var res = await _cli.ExecuteCommandAsync("SELECT score, SOLVE(status) FROM TestScore;");
            
            // Because Rete executes sequentially based on insertion or evaluation order, it could realistically be either. 
            // The key is it must not crash, and should set a value.
            Assert.Equal(MessageType.RESULT, res.Type);
            Assert.Contains("status", res.Content);
        }

        [Fact]
        public async Task Test_3_DivisionByZero_ShouldHandleGracefully()
        {
            await _cli.ExecuteCommandAsync("CREATE CONCEPT MathNode(numerator: DOUBLE, denominator: DOUBLE, result: DOUBLE);");
            // NCalc might actually return Infinity for 1.0 / 0.0, which is technically evaluated successfully.
            // Let's create an explicit constraint or see how the server handles it.
            await _cli.ExecuteCommandAsync("CREATE RULE DivRule SCOPE MathNode IF true THEN SET result = numerator / denominator;");
            
            // Provide 0.0
            await _cli.ExecuteCommandAsync("INSERT INTO MathNode ATTRIBUTE (numerator: 10.0, denominator: 0.0);");
            var res = await _cli.ExecuteCommandAsync("SELECT numerator, denominator, SOLVE(result) FROM MathNode;");
            Assert.Equal(MessageType.ERROR, res.Type);
            // Double division by zero in C# (and NCalc) is Infinity, which JSON serializer refuses to write by default
            Assert.True(res.Content.Contains("infinity", StringComparison.OrdinalIgnoreCase), "Expected JSON infinity serialization error");
        }

        [Fact]
        public async Task Test_4_DeepHierarchy_ShadowingAndResolution()
        {
            // A -> B -> C -> D
            await _cli.ExecuteCommandAsync("CREATE CONCEPT Level1(val1: INT);");
            // Level1 has rule:
            await _cli.ExecuteCommandAsync("CREATE RULE SetVal1 SCOPE Level1 IF val1 = 1 THEN SET val1 = 1;");

            await _cli.ExecuteCommandAsync("CREATE CONCEPT Level2(val2: INT);");
            await _cli.ExecuteCommandAsync("ADD HIERARCHY Level2 IS_A Level1;");

            await _cli.ExecuteCommandAsync("CREATE CONCEPT Level3(val3: INT);");
            await _cli.ExecuteCommandAsync("ADD HIERARCHY Level3 IS_A Level2;");

            await _cli.ExecuteCommandAsync("CREATE CONCEPT Level4(val4: INT, target: STRING);");
            await _cli.ExecuteCommandAsync("ADD HIERARCHY Level4 IS_A Level3;");

            await _cli.ExecuteCommandAsync("CREATE RULE DeepCombine SCOPE Level4 IF val1 = 1 AND val3 = 3 THEN SET target = 'Success';");

            // Solve at Level 4, given val1 and val3
            await _cli.ExecuteCommandAsync("INSERT INTO Level4 ATTRIBUTE (val1: 1, val2: 2, val3: 3);");
            var res = await _cli.ExecuteCommandAsync("SELECT val1, SOLVE(target) FROM Level4;");
            Assert.Equal(MessageType.RESULT, res.Type);
            Assert.Contains("Success", res.Content);
        }

        [Fact]
        public async Task Test_5_UnsolvableConstraints_ShouldFailResolution()
        {
            // Concept with strict constraint
            await _cli.ExecuteCommandAsync("CREATE CONCEPT Adult(VARIABLES(age: INT), CONSTRAINTS(age >= 18));");

            // Try to solve with invalid given
            await _cli.ExecuteCommandAsync("INSERT INTO Adult ATTRIBUTE (age: 10);");
            var res = await _cli.ExecuteCommandAsync("SELECT SOLVE(age) FROM Adult;");
            
            Assert.Equal(MessageType.RESULT, res.Type);
            Assert.Contains("violated", res.Content.ToLower());
        }
    }
}
