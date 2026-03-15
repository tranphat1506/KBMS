using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using KBMS.Network;
using KBMS.Models;

namespace KBMS.CLI;

public class Cli
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _isConnected;
    private readonly object _lock = new();

    public Cli(string host = "localhost", int port = 3307)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync(bool autoReconnect = true)
    {
        int retryCount = 0;
        int maxRetries = autoReconnect ? 5 : 1;

        while (retryCount < maxRetries)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_host, _port);
                _stream = _client.GetStream();
                _isConnected = true;
                Console.WriteLine($"Connected to KBMS Server at {_host}:{_port}");
                return;
            }
            catch (SocketException ex)
            {
                retryCount++;
                Console.WriteLine($"Connection failed (attempt {retryCount}/{maxRetries}): {ex.Message}");

                if (autoReconnect && retryCount < maxRetries)
                {
                    await Task.Delay(2000); // Wait 2 seconds before retry
                    continue;
                }
            }
        }

        _isConnected = false;
        Console.WriteLine("Failed to connect after all retries.");
        throw new Exception("Could not connect to server");
    }

    public async Task DisconnectAsync()
    {
        if (_stream != null)
        {
            try
            {
                await Protocol.SendMessageAsync(_stream, new Message
                {
                    Type = MessageType.LOGOUT,
                    Content = ""
                });
            }
            catch { /* Ignore errors during disconnect */ }
        }

        lock (_lock)
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            _stream = null;
            Console.WriteLine("Disconnected from server.");
        }
    }

    public async Task<Message?> ExecuteCommandAsync(string command)
    {
        if (_stream == null || !_isConnected)
        {
            Console.WriteLine("Not connected to server. Use 'CONNECT' command or wait for auto-reconnect.");
            return null;
        }

        try
        {
            // LOGIN command is special - send as LOGIN message type
            if (command.StartsWith("LOGIN", StringComparison.OrdinalIgnoreCase))
            {
                var message = new Message
                {
                    Type = MessageType.LOGIN,
                    Content = command
                };
                await Protocol.SendMessageAsync(_stream, message);
                return await Protocol.ReceiveMessageAsync(_stream);
            }

            // Other commands are QUERY
            var queryMessage = new Message
            {
                Type = MessageType.QUERY,
                Content = command
            };

            await Protocol.SendMessageAsync(_stream, queryMessage);
            return await Protocol.ReceiveMessageAsync(_stream);
        }
        catch (IOException)
        {
            Console.WriteLine("Server disconnected. Connection lost.");
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command: {ex.Message}");
            return new Message
            {
                Type = MessageType.ERROR,
                Content = $"Error: {ex.Message}"
            };
        }
    }

    public async Task StartInteractiveAsync(bool autoReconnect = true)
    {
        try
        {
            await ConnectAsync(autoReconnect);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: {ex.Message}");
            Console.WriteLine("CLI will continue in offline mode. Type 'CONNECT' to retry.");
        }

        Console.WriteLine("KBMS CLI v1.1");
        Console.WriteLine("Type 'HELP' for available commands, 'EXIT' to quit.");
        Console.WriteLine("Type 'CONNECT' to reconnect if connection is lost.\n");

        string? currentKb = null;
        string? currentUser = null;

        while (true)
        {
            string prompt = currentUser != null
                ? $"kbms{(currentKb != null ? $"/{currentKb}" : "")}> "
                : "login> ";

            Console.Write(prompt);
            var input = Console.ReadLine();

            // Handle EOF or non-interactive terminal
            if (input == null)
            {
                Console.WriteLine("\nExiting (EOF)...");
                break;
            }

            input = input.Trim();
            if (string.IsNullOrEmpty(input))
                continue;

            if (input.ToUpper() == "EXIT")
            {
                Console.WriteLine("Exiting...");
                break;
            }

            if (input.ToUpper() == "HELP")
            {
                ShowHelp();
                continue;
            }

            if (input.ToUpper() == "CLEAR")
            {
                Console.Clear();
                continue;
            }

            if (input.ToUpper() == "CONNECT")
            {
                Console.WriteLine("Attempting to reconnect...");
                await DisconnectAsync();
                await ConnectAsync(true); // Auto-reconnect
                continue;
            }

            // Handle LOGIN separately
            if (input.StartsWith("LOGIN", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Split(' ');
                if (parts.Length < 3)
                {
                    Console.WriteLine("Usage: LOGIN <username> <password>");
                    continue;
                }

                var username = parts[1];
                var password = parts[2];

                var msg = await ExecuteCommandAsync(input);
                if (msg?.Type == MessageType.RESULT && msg.Content.StartsWith("LOGIN_SUCCESS"))
                {
                    var resultParts = msg.Content.Split(':');
                    currentUser = resultParts[1];
                    Console.WriteLine($"Logged in as {currentUser} ({resultParts[2]})");
                }
                else if (msg?.Type == MessageType.ERROR)
                {
                    ResponseParser.DisplayError(msg.Content, input);
                }
                continue;
            }

            // Check if logged in
            if (currentUser == null)
            {
                Console.WriteLine("Please login first. Usage: LOGIN <username> <password>");
                continue;
            }

            var response = await ExecuteCommandAsync(input);

            if (response?.Type == MessageType.RESULT)
            {
                // Handle USE command
                if (input.StartsWith("USE", StringComparison.OrdinalIgnoreCase))
                {
                    currentKb = input.Split()[1];
                    Console.WriteLine($"Using knowledge base: {currentKb}");
                }
                else
                {
                    ResponseParser.DisplayResult(response, input);
                }
            }
            else if (response?.Type == MessageType.ERROR)
            {
                ResponseParser.DisplayError(response.Content, input);

                // If connection lost due to error, try auto-reconnect
                if (response.Content.Contains("disconnected", StringComparison.OrdinalIgnoreCase) ||
                    response.Content.Contains("Connection", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Attempting to reconnect...");
                    await Task.Delay(1000);
                    try
                    {
                        await ConnectAsync(true);
                        continue;
                    }
                    catch
                    {
                        Console.WriteLine("Reconnection failed. Please type 'CONNECT' to try again.");
                    }
                }
            }
        }

        await DisconnectAsync();
    }

    private void ShowHelp()
    {
        Console.WriteLine("KBMS CLI - Knowledge Base Management System");
        Console.WriteLine("============================================");
        Console.WriteLine();

        Console.WriteLine("CLI Commands:");
        Console.WriteLine("  LOGIN <username> <password>  - Login to server");
        Console.WriteLine("  CONNECT                      - Reconnect to server");
        Console.WriteLine("  CLEAR                        - Clear the screen");
        Console.WriteLine("  EXIT                         - Exit CLI");
        Console.WriteLine("  HELP                         - Show this help message");
        Console.WriteLine();

        Console.WriteLine("Knowledge Base Management:");
        Console.WriteLine("  CREATE KNOWLEDGE BASE <name> [DESCRIPTION '<desc>']");
        Console.WriteLine("  DROP KNOWLEDGE BASE <name>");
        Console.WriteLine("  USE <name>                   - Select current knowledge base");
        Console.WriteLine();

        Console.WriteLine("Concept Management:");
        Console.WriteLine("  CREATE CONCEPT <name>");
        Console.WriteLine("      VARIABLES (<var>:<type>, ...)");
        Console.WriteLine("      [ALIASES <alias1>, ...]");
        Console.WriteLine("      [BASE_OBJECTS <base1>, ...]");
        Console.WriteLine("      [CONSTRAINTS <expr>, ...]");
        Console.WriteLine("      [SAME_VARIABLES <v1>=<v2>, ...]");
        Console.WriteLine("  ADD VARIABLE <var>:<type> TO CONCEPT <name>");
        Console.WriteLine("  DROP CONCEPT <name>");
        Console.WriteLine();

        Console.WriteLine("Hierarchy Management:");
        Console.WriteLine("  ADD HIERARCHY <parent> IS_A <child>");
        Console.WriteLine("  ADD HIERARCHY <parent> PART_OF <child>");
        Console.WriteLine("  REMOVE HIERARCHY <parent> IS_A <child>");
        Console.WriteLine();

        Console.WriteLine("Relation Management:");
        Console.WriteLine("  CREATE RELATION <name> FROM <domain> TO <range>");
        Console.WriteLine("      [PROPERTIES <prop1>, ...]");
        Console.WriteLine("  DROP RELATION <name>");
        Console.WriteLine();

        Console.WriteLine("Operator & Function Management:");
        Console.WriteLine("  CREATE OPERATOR <sym> PARAMS (<type>, ...) RETURNS <type>");
        Console.WriteLine("  DROP OPERATOR <sym>");
        Console.WriteLine("  CREATE FUNCTION <name> PARAMS (<type> <param>, ...)");
        Console.WriteLine("      RETURNS <type> BODY '<formula>'");
        Console.WriteLine("  DROP FUNCTION <name>");
        Console.WriteLine();

        Console.WriteLine("Computation Management:");
        Console.WriteLine("  ADD COMPUTATION TO <concept>");
        Console.WriteLine("      VARIABLES <var1>, ..., <result>");
        Console.WriteLine("      FORMULA '<expr>' [COST <weight>]");
        Console.WriteLine("  REMOVE COMPUTATION <var> FROM <concept>");
        Console.WriteLine();

        Console.WriteLine("Rule Management:");
        Console.WriteLine("  CREATE RULE <name> [TYPE <type>] SCOPE <concept>");
        Console.WriteLine("      IF <hypothesis> THEN <conclusion> [COST <weight>]");
        Console.WriteLine("  DROP RULE <name>");
        Console.WriteLine();

        Console.WriteLine("Data Query (KBDML):");
        Console.WriteLine("  SELECT <concept> [WHERE <conditions>]");
        Console.WriteLine("  SELECT <concept> JOIN <relation> [WHERE <conditions>]");
        Console.WriteLine("  SELECT <concept> [ORDER BY <var> [ASC|DESC]]");
        Console.WriteLine("  SELECT <concept> [LIMIT <n> [OFFSET <m>]]");
        Console.WriteLine("  SELECT COUNT(*) FROM <concept> [WHERE ...]");
        Console.WriteLine("  SELECT SUM(<var>) FROM <concept> [WHERE ...]");
        Console.WriteLine("  SELECT AVG(<var>) FROM <concept> [WHERE ...]");
        Console.WriteLine("  SELECT MAX(<var>) FROM <concept> [WHERE ...]");
        Console.WriteLine("  SELECT MIN(<var>) FROM <concept> [WHERE ...]");
        Console.WriteLine();

        Console.WriteLine("Data Manipulation:");
        Console.WriteLine("  INSERT INTO <concept> VALUES (<field>=<value>, ...)");
        Console.WriteLine("  UPDATE <concept> SET <field>=<value>, ... [WHERE ...]");
        Console.WriteLine("  DELETE FROM <concept> [WHERE ...]");
        Console.WriteLine();

        Console.WriteLine("Reasoning:");
        Console.WriteLine("  SOLVE <concept> FOR <var> GIVEN <conditions> [USING <type>]");
        Console.WriteLine();

        Console.WriteLine("User Management:");
        Console.WriteLine("  CREATE USER <name> PASSWORD '<pass>' [ROLE <role>]");
        Console.WriteLine("  DROP USER <name>");
        Console.WriteLine();

        Console.WriteLine("Privilege Management:");
        Console.WriteLine("  GRANT <privilege> ON <kb> TO <user>");
        Console.WriteLine("  REVOKE <privilege> ON <kb> FROM <user>");
        Console.WriteLine("  Privilege types: READ, WRITE, ADMIN");
        Console.WriteLine();

        Console.WriteLine("Information Display:");
        Console.WriteLine("  SHOW KNOWLEDGE BASES");
        Console.WriteLine("  SHOW CONCEPTS [IN <kb>]");
        Console.WriteLine("  SHOW CONCEPT <name> [IN <kb>]");
        Console.WriteLine("  SHOW RULES [IN <kb>] [TYPE <type>]");
        Console.WriteLine("  SHOW RELATIONS [IN <kb>]");
        Console.WriteLine("  SHOW OPERATORS [IN <kb>]");
        Console.WriteLine("  SHOW FUNCTIONS [IN <kb>]");
        Console.WriteLine("  SHOW USERS");
        Console.WriteLine("  SHOW PRIVILEGES ON <kb>");
        Console.WriteLine("  SHOW PRIVILEGES OF <user>");
        Console.WriteLine();

        Console.WriteLine("Data Types:");
        Console.WriteLine("  Number: TINYINT, SMALLINT, INT, BIGINT, FLOAT, DOUBLE, DECIMAL(p,s)");
        Console.WriteLine("  String: VARCHAR(n), CHAR(n), TEXT");
        Console.WriteLine("  Boolean: BOOLEAN");
        Console.WriteLine("  Date/Time: DATE, DATETIME, TIMESTAMP");
        Console.WriteLine("  Reference: object");
        Console.WriteLine();

        Console.WriteLine("For detailed syntax, see docs/sql-syntax.md");
    }
}
