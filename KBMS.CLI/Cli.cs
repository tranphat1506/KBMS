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
    private readonly HistoryManager _history = new();
    private readonly LineEditor _editor = new();

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

            var input = _editor.ReadLine(prompt, _history.GetHistory());

            // Handle EOF or non-interactive terminal
            if (input == null)
            {
                Console.WriteLine("\nExiting (EOF)...");
                break;
            }

            input = input.Trim();
            if (string.IsNullOrEmpty(input))
                continue;

            // Add to history (privacy filter inside HistoryManager)
            _history.AddCommand(input);

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
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nKBMS CLI - Knowledge Base Management System (Enhanced)");
        Console.WriteLine("======================================================");
        Console.ResetColor();

        Console.WriteLine("\n[ Keypad Shortcuts ]");
        Console.WriteLine("  ↑ / ↓       - Navigate command history");
        Console.WriteLine("  ← / →       - Move cursor within the line");
        Console.WriteLine("  HOME / END  - Move cursor to start or end of line");
        Console.WriteLine("  ESC x2      - Clear current input");
        Console.WriteLine("  ENTER       - Execute command");

        Console.WriteLine("\n[ System Commands ]");
        Console.WriteLine("  LOGIN <u|p>     - Login to the server (Privacy Protected)");
        Console.WriteLine("  CONNECT         - Reconnect if connection is lost");
        Console.WriteLine("  CLEAR           - Clear terminal screen");
        Console.WriteLine("  HELP            - Show this guide");
        Console.WriteLine("  EXIT            - Quit the KBMS CLI");

        Console.WriteLine("\n[ Knowledge Base (KB) ]");
        Console.WriteLine("  CREATE KNOWLEDGE BASE <name> [DESCRIPTION '<txt>']");
        Console.WriteLine("  DROP KNOWLEDGE BASE <name>");
        Console.WriteLine("  USE <name>      - Set active KB for subsequent queries");
        Console.WriteLine("  SHOW KNOWLEDGE BASES");

        Console.WriteLine("\n[ Concepts & Schema ]");
        Console.WriteLine("  CREATE CONCEPT <name>");
        Console.WriteLine("    VARIABLES (v1:type, v2:type, ...)");
        Console.WriteLine("    [ALIASES a1, a2, ...]");
        Console.WriteLine("    [BASE_OBJECTS b1, ...]");
        Console.WriteLine("    [CONSTRAINTS expr1, ...]");
        Console.WriteLine("    [SAME_VARIABLES v1=v2, ...]");
        Console.WriteLine("    [CONSTRUCT_RELATIONS RelName(arg1, arg2), ...]");
        Console.WriteLine("  SHOW CONCEPTS [IN <kb>] / SHOW CONCEPT <name>");

        Console.WriteLine("\n[ Reasoning & Logic ]");
        Console.WriteLine("  CREATE RELATION <name> FROM <d> TO <r>");
        Console.WriteLine("    [PARAMS (p1, p2, ...)] [EQUATIONS eq1, ...]");
        Console.WriteLine("  CREATE RULE <name> [TYPE <t>] SCOPE <c> IF <hyp> THEN <conc>");
        Console.WriteLine("  ADD HIERARCHY <parent> [IS_A | PART_OF] <child>");
        Console.WriteLine("  SOLVE ON CONCEPT <name> GIVEN <facts> FIND <targets>");

        Console.WriteLine("\n[ Data Operations ]");
        Console.WriteLine("  INSERT INTO <concept> VALUES (f1=v1, f2=v2, ...)");
        Console.WriteLine("  SELECT <concept> [WHERE <cond>] [ORDER BY <f> ASC|DESC] [LIMIT <n>]");
        Console.WriteLine("  UPDATE <concept> SET f1=v1 [WHERE <cond>]");
        Console.WriteLine("  DELETE FROM <concept> [WHERE <cond>]");

        Console.WriteLine("\n[ Security & Users ]");
        Console.WriteLine("  CREATE USER <name> PASSWORD '<pass>' [ROLE <role>] (Privacy Protected)");
        Console.WriteLine("  GRANT <READ|WRITE|ADMIN> ON <kb> TO <user>");
        Console.WriteLine("  SHOW USERS / SHOW PRIVILEGES ON <kb>");

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("\nNote: Commands like LOGIN and CREATE USER are not stored in history for your safety.");
        Console.ResetColor();
        Console.WriteLine("======================================================\n");
    }
}
