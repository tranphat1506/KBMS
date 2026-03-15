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
                ? $"kbms{(currentKb != null ? "" : $"/{currentKb}")}> "
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
                    Console.WriteLine($"Error: {msg.Content}");
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
                    Console.WriteLine(response.Content);
                }
            }
            else if (response?.Type == MessageType.ERROR)
            {
                Console.WriteLine($"Error: {response.Content}");

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
        Console.WriteLine("Available Commands:");
        Console.WriteLine("  LOGIN <username> <password>     - Login to server");
        Console.WriteLine("  CONNECT                      - Reconnect to server (auto-reconnect)");
        Console.WriteLine("  CREATE KNOWLEDGE BASE <name>   - Create new knowledge base");
        Console.WriteLine("  DROP KNOWLEDGE BASE <name>     - Drop knowledge base");
        Console.WriteLine("  USE <name>                     - Select knowledge base");
        Console.WriteLine("  SELECT <concept> WHERE <cond>   - Query objects");
        Console.WriteLine("  INSERT INTO <concept> VALUES (...) - Insert object");
        Console.WriteLine("  UPDATE <concept> SET ...      - Update object");
        Console.WriteLine("  DELETE FROM <concept> WHERE ...   - Delete object");
        Console.WriteLine("  SOLVE <concept> KNOWN ... FIND - Solve reasoning");
        Console.WriteLine("  CREATE USER <name> PASSWORD <p>   - Create user");
        Console.WriteLine("  GRANT <privilege> ON <kb> TO <user> - Grant privilege");
        Console.WriteLine("  SHOW <type>                   - Show knowledge");
        Console.WriteLine("  EXIT                           - Exit CLI");
    }
}
