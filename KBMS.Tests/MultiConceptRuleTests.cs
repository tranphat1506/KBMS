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
    public class MultiConceptRuleTests : IDisposable
    {
        private readonly KbmsServer _server;
        private readonly Cli _cli;
        private readonly string _dataDir;
        private readonly ITestOutputHelper _output;

        public MultiConceptRuleTests(ITestOutputHelper output)
        {
            _output = output;
            _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "kbms_mcr_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);

            var port = 9600 + new Random().Next(100);
            _server = new KbmsServer("127.0.0.1", port, _dataDir);
            _ = _server.StartAsync();

            _cli = new Cli("127.0.0.1", port);
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    _cli.ConnectAsync().Wait();
                    break;
                }
                catch { Task.Delay(200).Wait(); }
            }
            _cli.ExecuteCommandAsync("LOGIN root root").Wait();
        }

        public void Dispose()
        {
            try { _cli?.DisconnectAsync().Wait(); } catch { }
            try { _server?.Stop(); } catch { }
            try { if (Directory.Exists(_dataDir)) Directory.Delete(_dataDir, true); } catch { }
        }

        [Fact]
        public async Task MultiConceptRule_CommaSyntax_ShouldParseAndCreate()
        {
            // Test comma-separated multi-concept scope syntax
            var result = await _cli.ExecuteCommandAsync(@"
                CREATE KNOWLEDGE BASE MedicalDB;
                USE MedicalDB;
                CREATE CONCEPT Patient(patientId: STRING, age: INT, riskLevel: STRING);
                CREATE CONCEPT LabResult(resultId: STRING, patientId: STRING, bloodSugar: DECIMAL, cholesterol: DECIMAL);
            ");

            Assert.Equal(MessageType.RESULT, result.Type);

            // Create multi-concept rule with comma syntax
            var ruleResult = await _cli.ExecuteCommandAsync(@"
                CREATE RULE CardiovascularRisk
                SCOPE Patient p, LabResult l
                IF p.age > 50 AND l.bloodSugar > 140
                THEN SET p.riskLevel = 'high'
                PRIORITY 80;
            ");

            _output.WriteLine($"Rule Result: {ruleResult?.Content}");
            Assert.Equal(MessageType.RESULT, ruleResult?.Type);
            Assert.Contains("CardiovascularRisk", ruleResult?.Content ?? "");
        }

        [Fact]
        public async Task MultiConceptRule_JoinSyntax_ShouldParseAndCreate()
        {
            // Test JOIN...ON multi-concept scope syntax
            var result = await _cli.ExecuteCommandAsync(@"
                CREATE KNOWLEDGE BASE RetailDB;
                USE RetailDB;
                CREATE CONCEPT Customer(customerId: STRING, name: STRING, totalSpent: DECIMAL, tier: STRING);
                CREATE CONCEPT Orders(orderId: STRING, customerId: STRING, amount: DECIMAL, status: STRING);
            ");

            Assert.Equal(MessageType.RESULT, result.Type);

            // Create multi-concept rule with JOIN...ON syntax
            var ruleResult = await _cli.ExecuteCommandAsync(@"
                CREATE RULE VipCustomer
                SCOPE Customer c JOIN Orders o ON c.customerId = o.customerId
                IF o.status = 'completed' AND o.amount > 1000
                THEN SET c.tier = 'VIP'
                PRIORITY 90;
            ");

            _output.WriteLine($"Rule Result: {ruleResult?.Content}");
            Assert.Equal(MessageType.RESULT, ruleResult?.Type);
            Assert.Contains("VipCustomer", ruleResult?.Content ?? "");
        }

        [Fact]
        public async Task SingleConceptRule_BackwardCompatible_ShouldWork()
        {
            // Ensure single-concept rules still work (backward compatibility)
            var result = await _cli.ExecuteCommandAsync(@"
                CREATE KNOWLEDGE BASE SimpleDB;
                USE SimpleDB;
                CREATE CONCEPT Triangle(a: DECIMAL, b: DECIMAL, c: DECIMAL, area: DECIMAL);
            ");

            Assert.Equal(MessageType.RESULT, result.Type);

            // Single concept rule (original syntax)
            var ruleResult = await _cli.ExecuteCommandAsync(@"
                CREATE RULE CalculateArea
                SCOPE Triangle
                IF a > 0 AND b > 0 AND c > 0
                THEN SET area = a * b * c / 2;
            ");

            _output.WriteLine($"Rule Result: {ruleResult?.Content}");
            Assert.Equal(MessageType.RESULT, ruleResult?.Type);
        }
    }
}
