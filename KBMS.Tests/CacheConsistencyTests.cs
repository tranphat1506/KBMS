using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using KBMS.CLI;
using KBMS.Server;
using KBMS.Network;

namespace KBMS.Tests
{
    public class CacheConsistencyTests : IAsyncLifetime
    {
        private KbmsServer _server;
        private Cli _cli;
        private string _dataDir;
        private int _port;

        public async Task InitializeAsync()
        {
            // Use a random port to avoid conflicts
            _port = new Random().Next(11000, 12000);
            _dataDir = Path.Combine(Path.GetTempPath(), "KBMS_CacheTest_" + Guid.NewGuid().ToString());
            _server = new KbmsServer("127.0.0.1", _port, _dataDir);
            _ = Task.Run(() => _server.StartAsync());
            
            await Task.Delay(2000); // Wait for server to start
            _cli = new Cli("127.0.0.1", _port);
            await _cli.ConnectAsync();
        }

        public async Task DisposeAsync()
        {
            if (_cli != null) await _cli.DisconnectAsync();
            if (_server != null) _server.Stop();
            
            // Cleanup data dir
            await Task.Delay(500);
            if (Directory.Exists(_dataDir)) 
            {
                try { Directory.Delete(_dataDir, true); } catch {}
            }
        }

        private async Task<Message> Exec(string cmd)
        {
            var res = await _cli.ExecuteCommandAsync(cmd);
            if (res == null) throw new Exception($"Command '{cmd}' returned null response");
            if (res.Content.ToLower().Contains("error"))
                throw new Exception($"Command '{cmd}' failed: {res.Content}");
            return res;
        }

        [Fact]
        public async Task FullLifecycle_ShouldNotHaveStaleCache()
        {
            string kbName = "ConsistencyTestKB";
            await Exec("LOGIN root root");
            await Exec($"CREATE KNOWLEDGE BASE {kbName};");
            await Exec($"USE {kbName};");
            
            await Exec("CREATE CONCEPT Person(id: STRING, name: STRING, age: NUMBER);");
            await Exec("CREATE RELATION ParentOf(Person, Person);");
            
            // Fixed syntax for single-concept rule
            await Exec("CREATE RULE R1 SCOPE Person IF id = id THEN SET age = age;");
            await Exec("INSERT INTO Person VARIABLES(id: '1', name: 'Alice', age: 70);");
            
            // Verify initial state
            var resC = await Exec("SHOW CONCEPTS;");
            Assert.Contains("Person", resC.Content);
            
            var resRules = await Exec("SHOW RULES;");
            Assert.Contains("R1", resRules.Content);

            // 3. DROP KB
            await Exec("USE system;");
            await Exec($"DROP KNOWLEDGE BASE {kbName};");

            // 4. RECREATE KB
            await Exec($"CREATE KNOWLEDGE BASE {kbName};");
            await Exec($"USE {kbName};");
            
            // Verify it's empty
            var resC2 = await Exec("SHOW CONCEPTS;");
            Assert.DoesNotContain("Person", resC2.Content);
            
            var resRules2 = await Exec("SHOW RULES;");
            Assert.DoesNotContain("R1", resRules2.Content);
        }

        [Fact]
        public async Task SolvableConsistency_ShouldClearEquations()
        {
            string kbName = "MathKB";
            await Exec("LOGIN root root");
            await Exec($"CREATE KNOWLEDGE BASE {kbName};");
            await Exec($"USE {kbName};");
            
            // Fixed syntax for EQUATIONS
            await Exec("CREATE CONCEPT Circle(r: NUMBER, a: NUMBER, EQUATIONS(a = 3.14 * r * r));");
            await Exec("INSERT INTO Circle VARIABLES(r: 10);");
            
            var resSolve = await Exec("SELECT SOLVE(a) FROM Circle;");
            Assert.Contains("314", resSolve.Content);

            // 2. DROP and RECREATE
            await Exec("USE system;");
            await Exec($"DROP KNOWLEDGE BASE {kbName};");
            await Exec($"CREATE KNOWLEDGE BASE {kbName};");
            await Exec($"USE {kbName};");
            
            // 3. Create DIFFERENT Equation
            await Exec("CREATE CONCEPT Circle(r: NUMBER, c: NUMBER, EQUATIONS(c = 6.28 * r));");
            await Exec("INSERT INTO Circle VARIABLES(r: 10);");

            // 4. Verify old equation is GONE
            var resOld = await _cli.ExecuteCommandAsync("SELECT SOLVE(a) FROM Circle;");
            Assert.DoesNotContain("314", resOld.Content);
            
            var resNew = await Exec("SELECT SOLVE(c) FROM Circle;");
            Assert.Contains("62.8", resNew.Content);
        }
    }
}
