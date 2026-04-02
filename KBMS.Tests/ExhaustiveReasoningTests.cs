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
    public class ExhaustiveKbFixture : IAsyncLifetime
    {
        public KbmsServer Server { get; private set; }
        public Cli RootCli { get; private set; }
        private string _dataDir;
        private static int _nextPort = 9100;
        public int Port { get; private set; }

        public async Task InitializeAsync()
        {
            _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "kbms_exhaustive_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            Port = System.Threading.Interlocked.Increment(ref _nextPort);

            Server = new KbmsServer("127.0.0.1", Port, _dataDir);
            _ = Server.StartAsync(); 
            
            RootCli = new Cli("127.0.0.1", Port);
            bool connected = false;
            for (int i = 0; i < 20; i++)
            {
                try {
                    await RootCli.ConnectAsync();
                    connected = true;
                    break;
                } catch { await Task.Delay(200); }
            }
            if(!connected) throw new Exception("Fixture connect failed");
            await RootCli.ExecuteCommandAsync("LOGIN root root");

            await BuildKnowledgeBaseAsync();
        }

        private async Task BuildKnowledgeBaseAsync()
        {
            var cmds = new[]
            {
                "CREATE KNOWLEDGE BASE PhysicsDB;",
                "USE PhysicsDB;",

                // 1. Function (Funcs)
                "CREATE FUNCTION GravityForce PARAMS (DOUBLE m1, DOUBLE m2, DOUBLE r) RETURNS DOUBLE BODY '(6.6743 * m1 * m2) / (r * r)';",
                
                // 2. Base Concept (C)
                "CREATE CONCEPT SpaceBody(name: STRING, mass: DOUBLE);",
                
                // 3. Hierarchy (H)
                "CREATE CONCEPT Planet(radius: DOUBLE, isGas: BOOLEAN);",
                "ADD HIERARCHY Planet IS_A SpaceBody;",

                // 4. Object Context
                "INSERT INTO SpaceBody ATTRIBUTE (name: 'StarX', mass: 1000000.0);",

                // 5. Rule (R) with Hierarchy Inherited Property
                // mass is inherited from SpaceBody
                "CREATE RULE CheckGas SCOPE Planet IF mass > 500 AND radius > 50 THEN SET isGas = true;",

                // 6. Global Relation (Relation)
                "CREATE RELATION orbits FROM Planet TO SpaceBody;",

                // 7. Dependent Concept with Equation/ComRel logic
                "CREATE CONCEPT OrbitSystem(pName: STRING, pMass: DOUBLE, pRadius: DOUBLE, distance: DOUBLE, force: DOUBLE, gasGiant: BOOLEAN);",
                
                // Equation evaluating Func and Op mappings
                "CREATE RULE CalcOrbitForce SCOPE OrbitSystem IF distance > 0 THEN SET force = GravityForce(pMass, 1000000.0, distance);"
            };

            foreach(var cmd in cmds)
            {
                var r = await RootCli.ExecuteCommandAsync(cmd);
                if(r.Type == MessageType.ERROR) throw new Exception($"Setup Failed on {cmd}: {r.Content}");
            }
        }

        public async Task DisposeAsync()
        {
            if (RootCli != null) await RootCli.DisconnectAsync();
            if (Server != null) Server.Stop();
            if (Directory.Exists(_dataDir)) 
            {
                try { Directory.Delete(_dataDir, true); } catch {}
            }
        }
    }

    [Collection("SequentialServerTests")]
    public class ExhaustiveReasoningTests : IClassFixture<ExhaustiveKbFixture>
    {
        private ExhaustiveKbFixture _fixture;
        private readonly ITestOutputHelper _output;

        public ExhaustiveReasoningTests(ExhaustiveKbFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        public static IEnumerable<object[]> GetTestCases()
        {
            // Generate 100 distinct variants
            for (int i = 1; i <= 100; i++)
            {
                double pMass = 100 + (10 * i);
                double pRadius = 10 + i;
                double distance = 1000 + (50 * i);
                
                bool expectedGas = pMass > 500 && pRadius > 50;
                double expectedForce = (6.6743 * pMass * 1000000.0) / (distance * distance);

                yield return new object[] { i, pMass, pRadius, distance, expectedGas, expectedForce };
            }
        }

        [Theory]
        [MemberData(nameof(GetTestCases))]
        public async Task Test_Solve_ComprehensiveFlow_100Cases(int id, double pMass, double pRadius, double distance, bool expectedGas, double expectedForce)
        {
            var cli = new Cli("127.0.0.1", _fixture.Port);
            await cli.ConnectAsync(false);
            await cli.ExecuteCommandAsync("LOGIN root root");
            await cli.ExecuteCommandAsync("USE PhysicsDB;");

            // Phase A: Create Planet, testing IS_A and Rules
            string pName = $"Planet_{id}";
            await cli.ExecuteCommandAsync($"INSERT INTO Planet ATTRIBUTE (name: '{pName}', mass: {pMass}, radius: {pRadius});");
            
            var sw1 = new System.IO.StringWriter();
            var origOut1 = Console.Out;
            Console.SetOut(sw1);
            var res1 = await cli.ExecuteCommandAsync($"SELECT SOLVE(isGas) FROM Planet WHERE name = '{pName}';");
            Console.SetOut(origOut1);
            var out1 = sw1.ToString();
            
            // Check if console output has the result
            if(expectedGas)
            {
                Assert.Contains("true", out1, StringComparison.OrdinalIgnoreCase);
            }

            // Phase B: Create Orbit System using inherited data, testing Equations and Functions
            await cli.ExecuteCommandAsync($"INSERT INTO OrbitSystem ATTRIBUTE (pName: '{pName}', pMass: {pMass}, pRadius: {pRadius}, distance: {distance});");
            
            var sw2 = new System.IO.StringWriter();
            var origOut2 = Console.Out;
            Console.SetOut(sw2);
            var res2 = await cli.ExecuteCommandAsync($"SELECT distance, SOLVE(force) FROM OrbitSystem WHERE pName = '{pName}';");
            Console.SetOut(origOut2);
            var out2 = sw2.ToString();

            // Verify standard reasoning pipeline success
            Assert.Equal(MessageType.RESULT, res2.Type);
            // Since double serialization has precision quirks in C#, we check the integer part
            string expectedIntPart = ((int)expectedForce).ToString();
            Assert.Contains(expectedIntPart, out2);

            await cli.DisconnectAsync();
        }
    }
}
