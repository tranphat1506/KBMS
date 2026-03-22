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
            
            bool fetchDone = false;
            Message? lastErrorResponse = null;
            Message? lastResultResponse = null;
            while (true)
            {
                var response = await Protocol.ReceiveMessageAsync(_stream);
                if (response == null) break;

                if (response.Type == MessageType.FETCH_DONE)
                {
                    ResponseParser.DisplayResult(response, command);
                    fetchDone = true;
                    break;
                }
                
                if (response.Type == MessageType.ERROR)
                {
                    ResponseParser.DisplayError(response.Content, command);
                    lastErrorResponse = response;
                    break;
                }

                // If it's a streaming component or normal command result, display it immediately and keep waiting
                if (response.Type == MessageType.METADATA || 
                    response.Type == MessageType.ROW || 
                    response.Type == MessageType.RESULT)
                {
                    ResponseParser.DisplayResult(response, command);
                    if (response.Type == MessageType.RESULT) lastResultResponse = response;
                    else if (response.Type == MessageType.ROW || response.Type == MessageType.METADATA)
                    {
                        if (lastResultResponse == null) lastResultResponse = new Message { Type = MessageType.RESULT, Content = "" };
                        lastResultResponse.Content += response.Content + "\n";
                    }
                    continue;
                }
            }
            if (lastErrorResponse != null) return lastErrorResponse;
            if (lastResultResponse != null) return lastResultResponse;
            if (fetchDone) return new Message { Type = MessageType.RESULT, Content = "Fetched" };
            return null;
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
        var commandBuilder = new System.Text.StringBuilder();

        while (true)
        {
            string prompt;
            if (commandBuilder.Length > 0)
            {
                // Line continuation prompt
                prompt = "    -> ";
            }
            else
            {
                prompt = currentUser != null
                    ? $"kbms{(currentKb != null ? $"/{currentKb}" : "")}> "
                    : "login> ";
            }

            var inputLine = _editor.ReadLine(prompt, _history.GetHistory());

            // Handle EOF or non-interactive terminal
            if (inputLine == null)
            {
                Console.WriteLine("\nExiting (EOF)...");
                break;
            }

            var trimmedLine = inputLine.Trim();
            if (string.IsNullOrEmpty(trimmedLine) && commandBuilder.Length == 0)
                continue;

            // Handle immediate meta-commands (no semicolon required, bypass buffer)
            var upperLine = trimmedLine.ToUpper();
            if (commandBuilder.Length == 0 && (upperLine == "EXIT" || upperLine == "HELP" || upperLine == "CLEAR" || upperLine == "CONNECT" || upperLine.StartsWith("LOGIN ")))
            {
                // Process immediately
                if (upperLine == "EXIT")
                {
                    Console.WriteLine("Exiting...");
                    break;
                }
                if (upperLine == "HELP")
                {
                    ShowHelp();
                    continue;
                }
                if (upperLine == "CLEAR")
                {
                    Console.Clear();
                    continue;
                }
                if (upperLine == "CONNECT")
                {
                    Console.WriteLine("Attempting to reconnect...");
                    await DisconnectAsync();
                    await ConnectAsync(true);
                    continue;
                }
                if (upperLine.StartsWith("LOGIN "))
                {
                    var loggedInUser = await HandleLoginCommand(trimmedLine);
                    if (loggedInUser != null) currentUser = loggedInUser;
                    continue;
                }
            }

            // Accumulate command
            commandBuilder.AppendLine(inputLine);

            // Check if command is finished (ends with ;)
            if (!trimmedLine.EndsWith(";"))
            {
                continue;
            }

            // Command is complete
            var fullCommand = commandBuilder.ToString().Trim();
            commandBuilder.Clear();

            // Add to history
            _history.AddCommand(fullCommand);

            // Check if logged in
            if (currentUser == null)
            {
                Console.WriteLine("Please login first. Usage: LOGIN <username> <password>");
                continue;
            }

            var response = await ExecuteCommandAsync(fullCommand);

            // Update CLI prompt if there was a USE command setup (we safely use Regex since the server accepted the command)
            if (response == null || response.Type != MessageType.ERROR)
            {
                var allMatches = System.Text.RegularExpressions.Regex.Matches(fullCommand, @"(?i)\bUSE\s+([a-zA-Z0-9_]+)\b");
                if (allMatches.Count > 0)
                {
                    currentKb = allMatches[allMatches.Count - 1].Groups[1].Value;
                    Console.WriteLine($"Using knowledge base: {currentKb} (Prompt Updated)");
                }
            }

            // If there's an error response indicating disconnect, auto-reconnect
            if (response?.Type == MessageType.ERROR)
            {
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

    private async Task<string?> HandleLoginCommand(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: LOGIN <username> <password>");
            return null;
        }

        var msg = await ExecuteCommandAsync(input);
        if (msg?.Type == MessageType.RESULT && msg.Content.StartsWith("LOGIN_SUCCESS"))
        {
            var resultParts = msg.Content.Split(':');
            var username = resultParts[1];
            Console.WriteLine($"Logged in as {username} ({resultParts[2]})");
            return username;
        }
        else if (msg?.Type == MessageType.ERROR)
        {
            ResponseParser.DisplayError(msg.Content, input);
        }
        return null;
    }
}
