using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using KBMS.Network;
using KBMS.Parser;
using KBMS.Parser.Ast;
using KBMS.Storage;
using KBMS.Models;
using KBMS.Knowledge;

namespace KBMS.Server;

public class KbmsServer
{
    private readonly string _host;
    private readonly int _port;
    private readonly AuthenticationManager _authManager;
    private readonly ConnectionManager _connectionManager;
    private readonly KnowledgeManager _knowledgeManager;
    private readonly StorageEngine _storage;
    private TcpListener? _listener;
    private bool _isRunning;

    public KbmsServer(
        string host = "0.0.0.0",
        int port = 3307,
        StorageEngine? storage = null)
    {
        _host = host;
        _port = port;
        _storage = storage ?? new StorageEngine("data", "kbms_encryption_key");
        _authManager = new AuthenticationManager(_storage);
        _connectionManager = new ConnectionManager();
        _knowledgeManager = new KnowledgeManager(_storage);
    }

    public async Task StartAsync()
    {
        var ipAddress = ResolveHost(_host);
        _listener = new TcpListener(ipAddress, _port);
        _listener.Start();
        _isRunning = true;

        Console.WriteLine($"KBMS Server started on {_host}:{_port}");

        while (_isRunning)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting connection: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener?.Stop();
    }

    public bool IsRunning => _isRunning && _listener != null;

    private async Task HandleClientAsync(TcpClient client)
    {
        var clientId = GenerateClientId();

        try
        {
            var session = _connectionManager.CreateSession(clientId);
            Console.WriteLine($"[{session.SessionId}] Client connected from {client.Client?.RemoteEndPoint}");

            using var stream = client.GetStream();

            while (client.Connected)
            {
                var message = await Protocol.ReceiveMessageAsync(stream);
                if (message == null)
                {
                    Console.WriteLine($"[{clientId}] Client disconnected gracefully");
                    break;
                }

                _connectionManager.UpdateActivity(clientId);

                var response = ProcessMessage(message, clientId);
                await Protocol.SendMessageAsync(stream, response);
            }
        }
        catch (IOException)
        {
            Console.WriteLine($"[{clientId}] Client disconnected: Connection lost");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{clientId}] Client error: {ex.Message}");
        }
        finally
        {
            _connectionManager.RemoveSession(clientId);
            client.Close();
            Console.WriteLine($"[{clientId}] Client disconnected");
        }
    }

    private Message ProcessMessage(Message message, string clientId)
    {
        try
        {
            switch (message.Type)
            {
                case MessageType.LOGIN:
                    return HandleLogin(message, clientId);

                case MessageType.QUERY:
                    return HandleQuery(message, clientId);

                case MessageType.LOGOUT:
                    return HandleLogout(message, clientId);

                default:
                    return new Message
                    {
                        Type = MessageType.ERROR,
                        Content = $"Unknown message type: {message.Type}"
                    };
            }
        }
        catch (Exception ex)
        {
            return new Message
            {
                Type = MessageType.ERROR,
                Content = $"Error: {ex.Message}"
            };
        }
    }

    private Message HandleLogin(Message message, string clientId)
    {
        var content = message.Content.Trim();

        // Remove "LOGIN" prefix if present
        if (content.StartsWith("LOGIN ", StringComparison.OrdinalIgnoreCase))
        {
            content = content.Substring(6).Trim();
        }

        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return new Message { Type = MessageType.ERROR, Content = "Invalid login format. Usage: LOGIN <username> <password>" };
        }

        var username = parts[0];
        var password = parts[1];

        var user = _authManager.Login(username, password);

        if (user == null)
        {
            return new Message { Type = MessageType.ERROR, Content = "Invalid credentials" };
        }

        _connectionManager.SetSessionUser(clientId, user);

        var session = _connectionManager.GetSession(clientId);
        return new Message
        {
            Type = MessageType.RESULT,
            Content = $"LOGIN_SUCCESS:{user.Username}:{user.Role}:{session?.SessionId}"
        };
    }

    private Message HandleQuery(Message message, string clientId)
    {
        var user = _connectionManager.GetCurrentUser(clientId);
        if (user == null)
        {
            return new Message { Type = MessageType.ERROR, Content = "Not authenticated. Please login first." };
        }

        var currentKb = _connectionManager.GetCurrentKb(clientId);
        try
        {
            var lexer = new KBMS.Parser.Lexer(message.Content);
            var tokens = lexer.Tokenize();
            var parser = new KBMS.Parser.Parser(tokens);
            var ast = parser.Parse();
            var result = _knowledgeManager.Execute(ast, user, currentKb);

            // Handle USE command - update session's current KB
            if (message.Content.StartsWith("USE", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the KB name from the USE command result
                if (result != null)
                {
                    var resultType = result.GetType();
                    var currentKbProp = resultType.GetProperty("currentKb");
                    var successProp = resultType.GetProperty("success");

                    var success = successProp?.GetValue(result) as bool?;
                    if (success == true && currentKbProp != null)
                    {
                        var kbName = currentKbProp.GetValue(result) as string;
                        if (kbName != null)
                        {
                            if (!_authManager.CheckPrivilege(user, "SELECT", kbName))
                            {
                                return new Message { Type = MessageType.ERROR, Content = "Permission denied" };
                            }
                            _connectionManager.SetSessionKb(clientId, kbName);
                        }
                    }
                }
            }

            // Check if result contains an error
            if (result != null)
            {
                var resultType = result.GetType();
                var errorProp = resultType.GetProperty("error");
                if (errorProp != null)
                {
                    var errorValue = errorProp.GetValue(result);
                    if (errorValue != null)
                    {
                        return new Message
                        {
                            Type = MessageType.ERROR,
                            Content = errorValue.ToString() ?? "Unknown error"
                        };
                    }
                }
            }

            return new Message
            {
                Type = MessageType.RESULT,
                Content = JsonSerializer.Serialize(result)
            };
        }
        catch (ParserException ex)
        {
            return new Message
            {
                Type = MessageType.ERROR,
                Content = $"Parse error: {ex}"
            };
        }
        catch (Exception ex)
        {
            return new Message
            {
                Type = MessageType.ERROR,
                Content = $"Error: {ex.Message}"
            };
        }
    }

    private Message HandleLogout(Message message, string clientId)
    {
        _connectionManager.SetSessionUser(clientId, null);
        return new Message { Type = MessageType.RESULT, Content = "LOGOUT_SUCCESS" };
    }

    private string GenerateClientId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"conn_{timestamp}_{random}";
    }

    private IPAddress ResolveHost(string host)
    {
        // Handle special cases
        if (host == "localhost" || host == "127.0.0.1")
        {
            return IPAddress.Loopback;
        }
        if (host == "0.0.0.0")
        {
            return IPAddress.Any;
        }

        // Try to parse as IP address first
        if (IPAddress.TryParse(host, out var ipAddress))
        {
            return ipAddress;
        }

        // Resolve hostname via DNS
        var addresses = System.Net.Dns.GetHostAddresses(host);
        return addresses.FirstOrDefault() ?? IPAddress.Any;
    }
}
