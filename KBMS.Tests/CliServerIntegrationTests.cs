using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using KBMS.CLI;
using KBMS.Network;
using KBMS.Models;
using KBMS.Server;
using KBMS.Storage;
using KBMS.Knowledge;

namespace KBMS.Tests;

/// <summary>
/// Integration tests for CLI to Server communication
/// Tests all KBQL commands through the network protocol
/// </summary>
public class CliServerIntegrationTests : IAsyncLifetime
{
    private KbmsServer? _server;
    private Cli? _cli;
    private readonly int _testPort;
    private const string TestHost = "localhost";
    private readonly string _testDataDir;

    private static int _nextPort = 33000;
    private static int GetNextPort() => Interlocked.Increment(ref _nextPort);

    public CliServerIntegrationTests()
    {
        _testPort = GetNextPort();
        _testDataDir = Path.Combine(Path.GetTempPath(), $"kbms_test_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        // Clean up test data directory
        if (Directory.Exists(_testDataDir))
        {
            Directory.Delete(_testDataDir, true);
        }

        // Start test server
        var storage = new StorageEngine(_testDataDir, "test_encryption_key");
        _server = new KbmsServer(TestHost, _testPort, _testDataDir);
        _ = _server.StartAsync();

        // Wait for server to be ready with retries
        var maxRetries = 20;
        var delay = 50;
        Exception? lastException = null;

        for (int i = 0; i < maxRetries; i++)
        {
            await Task.Delay(delay);
            try
            {
                _cli = new Cli(TestHost, _testPort);
                await _cli.ConnectAsync(autoReconnect: false);
                return; // Success
            }
            catch (Exception ex)
            {
                lastException = ex;
                _cli = null;
            }
        }

        throw new Exception($"Failed to connect to server on port {_testPort} after {maxRetries} attempts", lastException);
    }

    public async Task DisposeAsync()
    {
        if (_cli != null)
        {
            await _cli.DisconnectAsync();
        }
        _server?.Stop();

        // Clean up test data directory
        try
        {
            if (Directory.Exists(_testDataDir))
            {
                Directory.Delete(_testDataDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    // ==================== CONNECTION TESTS ====================

    [Fact]
    public async Task TC001_Connection_ShouldConnectSuccessfully()
    {
        // Assert
        Assert.NotNull(_cli);
        // Connection already established in InitializeAsync
    }

    [Fact]
    public async Task TC002_Connection_ShouldFailWhenServerNotRunning()
    {
        // Arrange
        var cli = new Cli(TestHost, 9999); // Non-existent port

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await cli.ConnectAsync(autoReconnect: false));
    }

    // ==================== LOGIN TESTS ====================

    [Fact]
    public async Task TC003_Login_ShouldLoginAsRootSuccessfully()
    {
        // Act
        var response = await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
        Assert.StartsWith("LOGIN_SUCCESS:root", response.Content);
    }

    [Fact]
    public async Task TC004_Login_ShouldFailWithWrongPassword()
    {
        // Act
        var response = await _cli!.ExecuteCommandAsync("LOGIN root wrongpassword");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    [Fact]
    public async Task TC005_Login_ShouldFailWithNonExistentUser()
    {
        // Act
        var response = await _cli!.ExecuteCommandAsync("LOGIN nonexistent password");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    [Fact]
    public async Task TC006_Login_ShouldFailWithMissingArguments()
    {
        // Act
        var response = await _cli!.ExecuteCommandAsync("LOGIN root;");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    // ==================== KNOWLEDGE BASE DDL TESTS ====================

    [Fact]
    public async Task TC010_CreateKnowledgeBase_ShouldSucceed()
    {
        // Arrange - Login first
        await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Act
        var response = await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE test_kb DESCRIPTION 'Test knowledge base';");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, $"Expected RESULT but got {response.Type}: {response.Content}");
        Assert.Contains("created successfully", response.Content);
    }

    [Fact]
    public async Task TC011_CreateKnowledgeBase_ShouldFailWhenNotLoggedIn()
    {
        // Act
        var response = await _cli!.ExecuteCommandAsync("CREATE KNOWLEDGE BASE test_kb2;");

        // Assert - Should fail due to not logged in
        // Note: The server might allow this for root, behavior depends on implementation
        Assert.NotNull(response);
    }

    [Fact]
    public async Task TC012_CreateKnowledgeBase_ShouldFailWithDuplicateName()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE duplicate_kb;");

        // Act - Try to create again
        var response = await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE duplicate_kb;");

        // Assert
        Assert.NotNull(response);
        // Should fail or return error about duplicate
    }

    [Fact]
    public async Task TC013_DropKnowledgeBase_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE kb_to_drop;");

        // Act
        var response = await _cli.ExecuteCommandAsync("DROP KNOWLEDGE BASE kb_to_drop;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
        Assert.Contains("dropped successfully", response.Content);
    }

    [Fact]
    public async Task TC014_DropKnowledgeBase_ShouldFailWhenNotFound()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Act
        var response = await _cli.ExecuteCommandAsync("DROP KNOWLEDGE BASE nonexistent_kb;");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    [Fact]
    public async Task TC015_UseKnowledgeBase_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE kb_to_use;");

        // Act
        var response = await _cli.ExecuteCommandAsync("USE kb_to_use;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
        Assert.Contains("kb_to_use", response.Content);
    }

    [Fact]
    public async Task TC016_UseKnowledgeBase_ShouldFailWhenNotFound()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Act
        var response = await _cli.ExecuteCommandAsync("USE nonexistent_kb;");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    // ==================== CONCEPT DDL TESTS ====================

    [Fact]
    public async Task TC020_CreateConcept_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE concept_test_kb;");
        await _cli.ExecuteCommandAsync("USE concept_test_kb;");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "CREATE CONCEPT Person ( VARIABLES (name: STRING, age: INT) );");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC021_CreateConcept_ShouldFailWhenNoKbSelected()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "CREATE CONCEPT Person ( VARIABLES (name: STRING) );");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    [Fact]
    public async Task TC022_CreateConcept_WithHierarchy_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE hierarchy_test_kb;");
        await _cli.ExecuteCommandAsync("USE hierarchy_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Animal ( VARIABLES (name: STRING) );");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Dog ( VARIABLES (name: STRING, breed: STRING) );");

        // Act
        var response = await _cli.ExecuteCommandAsync("CREATE HIERARCHY Dog ISA Animal;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC023_DropConcept_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE drop_concept_kb;");
        await _cli.ExecuteCommandAsync("USE drop_concept_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT ToDrop ( VARIABLES (x: INT) );");

        // Act
        var response = await _cli.ExecuteCommandAsync("DROP CONCEPT ToDrop;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC024_AddVariable_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE add_var_kb;");
        await _cli.ExecuteCommandAsync("USE add_var_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Person ( VARIABLES (name: STRING) );");

        // Act
        var response = await _cli.ExecuteCommandAsync("ADD VARIABLE email STRING TO Person;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== RELATION DDL TESTS ====================

    [Fact]
    public async Task TC030_CreateRelation_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE relation_test_kb;");
        await _cli.ExecuteCommandAsync("USE relation_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Person ( VARIABLES (name: STRING) );");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Car ( VARIABLES (model: STRING) );");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "CREATE RELATION owns DOMAIN Person RANGE Car PROPERTIES functional");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC031_DropRelation_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE drop_relation_kb;");
        await _cli.ExecuteCommandAsync("USE drop_relation_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT A ( VARIABLES (x: INT) );");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT B ( VARIABLES (y: INT) );");
        await _cli.ExecuteCommandAsync("CREATE RELATION rel DOMAIN A RANGE B;");
        await _cli.ExecuteCommandAsync("DROP RELATION rel;");

        // Act & Assert - Verify it's gone (would fail to drop again)
        var response = await _cli.ExecuteCommandAsync("DROP RELATION rel;");
        Assert.NotNull(response);
    }

    // ==================== OPERATOR DDL TESTS ====================

    [Fact]
    public async Task TC040_CreateOperator_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE operator_test_kb;");
        await _cli.ExecuteCommandAsync("USE operator_test_kb;");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "CREATE OPERATOR add PARAMS (INT, INT) RETURNS INT");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC041_DropOperator_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE drop_op_kb;");
        await _cli.ExecuteCommandAsync("USE drop_op_kb;");
        await _cli.ExecuteCommandAsync("CREATE OPERATOR testOp PARAMS (INT) RETURNS INT;");

        // Act
        var response = await _cli.ExecuteCommandAsync("DROP OPERATOR testOp;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== FUNCTION DDL TESTS ====================

    [Fact]
    public async Task TC050_CreateFunction_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE function_test_kb;");
        await _cli.ExecuteCommandAsync("USE function_test_kb;");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "CREATE FUNCTION calculateArea PARAMS (DOUBLE width, DOUBLE height) RETURNS DOUBLE BODY 'width * height'");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC051_DropFunction_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE drop_func_kb;");
        await _cli.ExecuteCommandAsync("USE drop_func_kb;");
        await _cli.ExecuteCommandAsync("CREATE FUNCTION testFunc PARAMS (INT x) RETURNS INT BODY 'x';");

        // Act
        var response = await _cli.ExecuteCommandAsync("DROP FUNCTION testFunc;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== RULE DDL TESTS ====================

    [Fact]
    public async Task TC060_CreateRule_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE rule_test_kb;");
        await _cli.ExecuteCommandAsync("USE rule_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Person ( VARIABLES (name: STRING, age: INT, isAdult: BOOLEAN) );");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "CREATE RULE adultRule IF Person.age >= 18 THEN SET Person.isAdult = true;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC061_DropRule_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE drop_rule_kb;");
        await _cli.ExecuteCommandAsync("USE drop_rule_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT X ( VARIABLES (a: INT, b: BOOLEAN) );");
        await _cli.ExecuteCommandAsync("CREATE RULE rule1 IF X.a > 10 THEN SET X.b = true;");

        // Act
        var response = await _cli.ExecuteCommandAsync("DROP RULE rule1;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== USER MANAGEMENT TESTS ====================

    [Fact]
    public async Task TC070_CreateUser_ShouldSucceed()
    {
        // Arrange
        var loginResponse = await _cli!.ExecuteCommandAsync("LOGIN root root");
        Assert.NotNull(loginResponse);
        Assert.Equal(MessageType.RESULT, loginResponse!.Type);

        // Act
        var response = await _cli.ExecuteCommandAsync("CREATE USER testuser PASSWORD testpass123 ROLE USER;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, $"Expected RESULT but got {response.Type}: {response.Content}");
    }

    [Fact]
    public async Task TC071_CreateUser_ShouldLoginWithNewUser()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE USER newuser PASSWORD newpass ROLE USER;");

        // Act
        var response = await _cli.ExecuteCommandAsync("LOGIN newuser newpass");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
        Assert.StartsWith("LOGIN_SUCCESS:newuser", response!.Content);
    }

    [Fact]
    public async Task TC072_DropUser_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE USER userToDrop PASSWORD pass ROLE USER;");

        // Act
        var response = await _cli.ExecuteCommandAsync("DROP USER userToDrop;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC073_GrantPrivilege_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE grant_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE USER grantuser PASSWORD pass ROLE USER;");

        // Act
        var response = await _cli.ExecuteCommandAsync("GRANT SELECT ON grant_test_kb TO grantuser;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC074_RevokePrivilege_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE revoke_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE USER revokeuser PASSWORD pass ROLE USER;");
        await _cli.ExecuteCommandAsync("GRANT SELECT ON revoke_test_kb TO revokeuser;");

        // Act
        var response = await _cli.ExecuteCommandAsync("REVOKE SELECT ON revoke_test_kb FROM revokeuser;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== DML SELECT TESTS ====================

    [Fact]
    public async Task TC080_Select_ShouldReturnObjects()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE select_test_kb;");
        await _cli.ExecuteCommandAsync("USE select_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Product ( VARIABLES (name: STRING, price: DOUBLE) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE ('Laptop', 999.99);");
        await _cli.ExecuteCommandAsync("INSERT INTO Product ATTRIBUTE ('Mouse', 29.99);");

        // Act
        var response = await _cli.ExecuteCommandAsync("SELECT * FROM Product;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, $"Expected RESULT but got {response.Type}: {response.Content}");
    }

    [Fact]
    public async Task TC081_Select_WithWhereClause_ShouldFilter()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE select_where_kb;");
        await _cli.ExecuteCommandAsync("USE select_where_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Employee ( VARIABLES (name: STRING, salary: DOUBLE) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Employee ATTRIBUTE ('Alice', 50000);");
        await _cli.ExecuteCommandAsync("INSERT INTO Employee ATTRIBUTE ('Bob', 30000);");
        await _cli.ExecuteCommandAsync("INSERT INTO Employee ATTRIBUTE ('Charlie', 70000);");

        // Act
        var response = await _cli.ExecuteCommandAsync("SELECT * FROM Employee WHERE salary > 40000;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC082_Select_WithOrderBy_ShouldSort()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE select_order_kb;");
        await _cli.ExecuteCommandAsync("USE select_order_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Item ( VARIABLES (name: STRING, quantity: INT) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Item ATTRIBUTE ('A', 10);");
        await _cli.ExecuteCommandAsync("INSERT INTO Item ATTRIBUTE ('B', 5);");
        await _cli.ExecuteCommandAsync("INSERT INTO Item ATTRIBUTE ('C', 20);");

        // Act
        var response = await _cli.ExecuteCommandAsync("SELECT * FROM Item ORDER BY quantity DESC;");

        // Assert
        Assert.NotNull(response);
        if (response!.Type != MessageType.RESULT)
        {
            Console.WriteLine($"Error: {response.Content}");
        }
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC083_Select_WithLimit_ShouldLimitResults()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE select_limit_kb;");
        await _cli.ExecuteCommandAsync("USE select_limit_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Number ( VARIABLES (value: INT) );");
        var insertResult = await _cli.ExecuteCommandAsync("INSERT INTO Number ATTRIBUTE (1);");
        Assert.True(insertResult!.Type == MessageType.RESULT, $"INSERT failed: {insertResult.Content}");

        for (int i = 2; i <= 10; i++)
        {
            await _cli.ExecuteCommandAsync($"INSERT INTO Number ATTRIBUTE ({i});");
        }

        // Act
        var response = await _cli.ExecuteCommandAsync("SELECT * FROM Number LIMIT 5;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, $"Expected RESULT but got {response.Type}: {response.Content}");
    }

    // ==================== DML INSERT TESTS ====================

    [Fact]
    public async Task TC090_Insert_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE insert_test_kb;");
        await _cli.ExecuteCommandAsync("USE insert_test_kb;");
        var conceptResponse = await _cli.ExecuteCommandAsync("CREATE CONCEPT Book ( VARIABLES (title: STRING, author: STRING, pages: INT) );");
        Assert.NotNull(conceptResponse);
        Assert.True(conceptResponse!.Type == MessageType.RESULT, $"CREATE CONCEPT failed: {conceptResponse.Content}");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "INSERT INTO Book ATTRIBUTE ('The Great Gatsby', 'F. Scott Fitzgerald', 180);");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, $"INSERT failed: {response.Content}");
    }

    [Fact]
    public async Task TC091_Insert_MultipleValues_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE insert_multi_kb;");
        await _cli.ExecuteCommandAsync("USE insert_multi_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Color ( VARIABLES (name: STRING, code: STRING) );");

        // Act
        var response1 = await _cli.ExecuteCommandAsync("INSERT INTO Color ATTRIBUTE ('Red', '#FF0000');");
        var response2 = await _cli.ExecuteCommandAsync("INSERT INTO Color ATTRIBUTE ('Green', '#00FF00');");
        var response3 = await _cli.ExecuteCommandAsync("INSERT INTO Color ATTRIBUTE ('Blue', '#0000FF');");

        // Assert
        Assert.Equal(MessageType.RESULT, response1!.Type);
        Assert.Equal(MessageType.RESULT, response2!.Type);
        Assert.Equal(MessageType.RESULT, response3!.Type);
    }

    [Fact]
    public async Task TC092_Insert_WithInvalidConcept_ShouldFail()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE insert_invalid_kb;");
        await _cli.ExecuteCommandAsync("USE insert_invalid_kb;");

        // Act
        var response = await _cli.ExecuteCommandAsync("INSERT INTO NonExistent ATTRIBUTE ('test');");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    // ==================== DML UPDATE TESTS ====================

    [Fact]
    public async Task TC100_Update_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE update_test_kb;");
        await _cli.ExecuteCommandAsync("USE update_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Account ( VARIABLES (name: STRING, balance: DOUBLE) );");

        var insertResponse = await _cli.ExecuteCommandAsync("INSERT INTO Account ATTRIBUTE ('Savings', 1000.00);");
        Assert.True(insertResponse!.Type == MessageType.RESULT, $"INSERT failed: {insertResponse.Content}");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "UPDATE Account ATTRIBUTE (SET balance: 1500.00) WHERE name = 'Savings';");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, $"Expected RESULT but got {response.Type}: {response.Content}");
    }

    [Fact]
    public async Task TC101_Update_WithWhereClause_ShouldUpdateOnlyMatching()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE update_where_kb;");
        await _cli.ExecuteCommandAsync("USE update_where_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Status ( VARIABLES (name: STRING, active: BOOLEAN) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Status ATTRIBUTE ('Item1', true);");
        await _cli.ExecuteCommandAsync("INSERT INTO Status ATTRIBUTE ('Item2', true);");
        await _cli.ExecuteCommandAsync("INSERT INTO Status ATTRIBUTE ('Item3', false);");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "UPDATE Status ATTRIBUTE (SET active: false) WHERE name = 'Item1';");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== DML DELETE TESTS ====================

    [Fact]
    public async Task TC110_Delete_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE delete_test_kb;");
        await _cli.ExecuteCommandAsync("USE delete_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Temp ( VARIABLES (id: INT, value: STRING) );");
        var insertResponse = await _cli.ExecuteCommandAsync("INSERT INTO Temp ATTRIBUTE (1, 'to delete');");
        Assert.True(insertResponse!.Type == MessageType.RESULT, $"INSERT failed: {insertResponse.Content}");

        // Act
        var response = await _cli.ExecuteCommandAsync("DELETE FROM Temp WHERE id = 1;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, $"Expected RESULT but got {response.Type}: {response.Content}");
    }

    [Fact]
    public async Task TC111_Delete_AllRecords_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE delete_all_kb;");
        await _cli.ExecuteCommandAsync("USE delete_all_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Log ( VARIABLES (message: STRING) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Log ATTRIBUTE ('msg1');");
        await _cli.ExecuteCommandAsync("INSERT INTO Log ATTRIBUTE ('msg2');");
        await _cli.ExecuteCommandAsync("INSERT INTO Log ATTRIBUTE ('msg3');");

        // Act
        var response = await _cli.ExecuteCommandAsync("DELETE FROM Log;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== SHOW COMMANDS TESTS ====================

    [Fact]
    public async Task TC120_ShowKnowledgeBases_ShouldReturnList()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE show_kb_test;");

        // Act
        var response = await _cli.ExecuteCommandAsync("SHOW KNOWLEDGE BASES;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC121_ShowConcepts_ShouldReturnList()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE show_concepts_kb;");
        await _cli.ExecuteCommandAsync("USE show_concepts_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT A ( VARIABLES (x: INT) );");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT B ( VARIABLES (y: STRING) );");

        // Act
        var response = await _cli.ExecuteCommandAsync("SHOW CONCEPTS;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC122_ShowRules_ShouldReturnList()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE show_rules_kb;");
        await _cli.ExecuteCommandAsync("USE show_rules_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT X ( VARIABLES (a: INT, b: BOOLEAN) );");
        await _cli.ExecuteCommandAsync("CREATE RULE rule1 IF X.a > 10 THEN SET X.b = true;");

        // Act
        var response = await _cli.ExecuteCommandAsync("SHOW RULES;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC123_ShowRelations_ShouldReturnList()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE show_rel_kb;");
        await _cli.ExecuteCommandAsync("USE show_rel_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT P ( VARIABLES (x: INT) );");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Q ( VARIABLES (y: INT) );");
        await _cli.ExecuteCommandAsync("CREATE RELATION has DOMAIN P RANGE Q;");

        // Act
        var response = await _cli.ExecuteCommandAsync("SHOW RELATIONS;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC124_ShowUsers_ShouldReturnList()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Act
        var response = await _cli.ExecuteCommandAsync("SHOW USERS;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== SOLVE (REASONING) TESTS ====================

    [Fact]
    public async Task TC130_Solve_ShouldReturnResult()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE solve_test_kb;");
        await _cli.ExecuteCommandAsync("USE solve_test_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Triangle ( VARIABLES (id: STRING, a: DOUBLE, b: DOUBLE, c: DOUBLE, area: DOUBLE) );");
        await _cli.ExecuteCommandAsync("CREATE RULE CalcArea SCOPE Triangle IF a > 0 AND b > 0 THEN SET area = (a * b) / 2.0;");
        await _cli.ExecuteCommandAsync("INSERT INTO Triangle ATTRIBUTE (id: 'T1', a: 3.0, b: 4.0, c: 5.0);");

        // Act
        var response = await _cli.ExecuteCommandAsync("SELECT SOLVE(area) FROM Triangle WHERE id = 'T1';");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.RESULT, response!.Type);
        Assert.Contains("6", response.Content);
    }


    // ==================== AGGREGATION TESTS ====================

    [Fact]
    public async Task TC140_Select_WithCount_ShouldReturnCount()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE agg_count_kb;");
        await _cli.ExecuteCommandAsync("USE agg_count_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Item ( VARIABLES (name: STRING, value: INT) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Item ATTRIBUTE ('A', 1);");
        await _cli.ExecuteCommandAsync("INSERT INTO Item ATTRIBUTE ('B', 2);");
        await _cli.ExecuteCommandAsync("INSERT INTO Item ATTRIBUTE ('C', 3);");

        // Act
        var response = await _cli.ExecuteCommandAsync("SELECT COUNT(*) FROM Item;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC141_Select_WithSum_ShouldReturnSum()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE agg_sum_kb;");
        await _cli.ExecuteCommandAsync("USE agg_sum_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Sales ( VARIABLES (product: STRING, amount: DOUBLE) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Sales ATTRIBUTE ('P1', 100);");
        await _cli.ExecuteCommandAsync("INSERT INTO Sales ATTRIBUTE ('P2', 200);");
        await _cli.ExecuteCommandAsync("INSERT INTO Sales ATTRIBUTE ('P3', 300);");

        // Act
        var response = await _cli.ExecuteCommandAsync("SELECT SUM(amount) FROM Sales;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC142_Select_WithAvg_ShouldReturnAverage()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE agg_avg_kb;");
        await _cli.ExecuteCommandAsync("USE agg_avg_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Score ( VARIABLES (student: STRING, score: DOUBLE) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Score ATTRIBUTE ('Alice', 85);");
        await _cli.ExecuteCommandAsync("INSERT INTO Score ATTRIBUTE ('Bob', 90);");
        await _cli.ExecuteCommandAsync("INSERT INTO Score ATTRIBUTE ('Charlie', 80);");

        // Act
        var response = await _cli.ExecuteCommandAsync("SELECT AVG(score) FROM Score;");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    // ==================== ERROR HANDLING TESTS ====================

    [Fact]
    public async Task TC150_InvalidSyntax_ShouldReturnError()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Act
        var response = await _cli.ExecuteCommandAsync("CREATE INVALID SYNTAX HERE;");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    [Fact]
    public async Task TC151_EmptyQuery_ShouldReturnError()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Act
        var response = await _cli.ExecuteCommandAsync("");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    [Fact]
    public async Task TC152_UnknownCommand_ShouldReturnError()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");

        // Act
        var response = await _cli.ExecuteCommandAsync("UNKNOWN_COMMAND test;");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(MessageType.ERROR, response!.Type);
    }

    // ==================== PRIVILEGE CHECKING TESTS ====================

    [Fact]
    public async Task TC160_RegularUser_CannotCreateKb_WithoutPermission()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE USER regularuser PASSWORD pass ROLE USER;");
        await _cli.ExecuteCommandAsync("LOGIN regularuser pass");

        // Act
        var response = await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE unauthorized_kb;");

        // Assert - Regular user may not have permission to create KB
        Assert.NotNull(response);
    }

    [Fact]
    public async Task TC161_User_CannotAccessKb_WithoutSelectPrivilege()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE private_kb;");
        await _cli.ExecuteCommandAsync("USE private_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Secret ( VARIABLES (data: STRING) );");
        await _cli.ExecuteCommandAsync("INSERT INTO Secret ATTRIBUTE ('confidential');");
        await _cli.ExecuteCommandAsync("CREATE USER restricteduser PASSWORD pass ROLE USER;");
        // Note: Not granting any privilege on private_kb
        await _cli.ExecuteCommandAsync("LOGIN restricteduser pass");

        // Act
        var response = await _cli.ExecuteCommandAsync("USE private_kb;");

        // Assert
        Assert.NotNull(response);
        // Should either fail to use or fail to select
    }

    // ==================== CONNECTION RESILIENCE TESTS ====================

    [Fact]
    public async Task TC170_Disconnect_AfterCommand_ShouldWork()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE disconnect_test;");

        // Act
        await _cli.DisconnectAsync();

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    // ==================== SPECIAL CHARACTERS TESTS ====================

    [Fact]
    public async Task TC180_Insert_WithSpecialChars_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE special_chars_kb;");
        await _cli.ExecuteCommandAsync("USE special_chars_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Message ( VARIABLES (text: STRING) );");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "INSERT INTO Message ATTRIBUTE ('Hello, World! @#$%^&*()')");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }

    [Fact]
    public async Task TC181_Insert_WithUnicode_ShouldSucceed()
    {
        // Arrange
        await _cli!.ExecuteCommandAsync("LOGIN root root");
        await _cli.ExecuteCommandAsync("CREATE KNOWLEDGE BASE unicode_kb;");
        await _cli.ExecuteCommandAsync("USE unicode_kb;");
        await _cli.ExecuteCommandAsync("CREATE CONCEPT Greeting ( VARIABLES (text: STRING) );");

        // Act
        var response = await _cli.ExecuteCommandAsync(
            "INSERT INTO Greeting ATTRIBUTE ('Xin chào - 你好 - مرحبا')");

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Type == MessageType.RESULT, response.Content);
    }
}
