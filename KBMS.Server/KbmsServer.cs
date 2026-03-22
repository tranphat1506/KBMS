using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using KBMS.Network;
using KBMS.Parser;
using KBMS.Parser.Ast;
using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
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
    private readonly Logger _logger;
    private TcpListener? _listener;
    private bool _isRunning;
    
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static string ToJson(object obj)
    {
        return JsonSerializer.Serialize(obj, _jsonOptions);
    }

    public KbmsServer(
        string host = "0.0.0.0",
        int port = 3307,
        StorageEngine? storage = null)
    {
        _host = host;
        _port = port;
        _storage = storage ?? new StorageEngine("data", "kbms_encryption_key");
        _logger = new Logger(LogLevel.Info);
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

        _logger.Info("SERVER", $"KBMS Server started on {_host}:{_port}");

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
            _logger.Info(session.SessionId, $"Client connected from {client.Client?.RemoteEndPoint}");

            using var stream = client.GetStream();

            while (client.Connected)
            {
                var message = await Protocol.ReceiveMessageAsync(stream);
                if (message == null)
                {
                    _logger.Info(clientId, "Client disconnected gracefully");
                    break;
                }

                _connectionManager.UpdateActivity(clientId);

                var responses = await ProcessMessageAsync(message, clientId, stream);
                foreach (var response in responses)
                {
                    await Protocol.SendMessageAsync(stream, response);
                }
            }
        }
        catch (IOException)
        {
            _logger.Info(clientId, "Client disconnected: Connection lost");
        }
        catch (Exception ex)
        {
            _logger.Error(clientId, $"Client error: {ex.Message}", ex);
        }
        finally
        {
            _connectionManager.RemoveSession(clientId);
            client.Close();
            _logger.Info(clientId, "Client disconnected");
        }
    }

    private async Task<IEnumerable<Message>> ProcessMessageAsync(Message message, string clientId, Stream stream)
    {
        try
        {
            var user = _connectionManager.GetCurrentUser(clientId);
            var username = user?.Username;

            switch (message.Type)
            {
                case MessageType.LOGIN:
                    return new[] { HandleLogin(message, clientId) };

                case MessageType.QUERY:
                    return await HandleQueryAsync(message, clientId, stream);

                case MessageType.LOGOUT:
                    return new[] { HandleLogout(message, clientId) };

                default:
                    return new[] { new Message
                    {
                        Type = MessageType.ERROR,
                        Content = ToJson(ErrorResponse.RuntimeErrorResponse(new Exception($"Unknown message type: {message.Type}"), ""))
                    } };
            }
        }
        catch (Exception ex)
        {
            _logger.Error(clientId, $"ProcessMessage error: {ex.Message}", ex);
            return new[] { new Message
            {
                Type = MessageType.ERROR,
                Content = ToJson(ErrorResponse.RuntimeErrorResponse(ex, ""))
            } };
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

        _logger.LogRequest(clientId, message.Type.ToString());

        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return new Message
            {
                Type = MessageType.ERROR,
                Content = ToJson(ErrorResponse.AuthenticationErrorResponse("Invalid login format. Usage: LOGIN <username> <password>"))
            };
        }

        var username = parts[0];
        var password = parts[1];

        var user = _authManager.Login(username, password);

        if (user == null)
        {
            return new Message
            {
                Type = MessageType.ERROR,
                Content = ToJson(ErrorResponse.AuthenticationErrorResponse("Invalid credentials"))
            };
        }

        _connectionManager.SetSessionUser(clientId, user);

        var session = _connectionManager.GetSession(clientId);
        return new Message
        {
            Type = MessageType.RESULT,
            Content = $"LOGIN_SUCCESS:{user.Username}:{user.Role}:{session?.SessionId}"
        };
    }

    private async Task<IEnumerable<Message>> HandleQueryAsync(Message message, string clientId, Stream stream)
    {
        var user = _connectionManager.GetCurrentUser(clientId);
        if (user == null)
        {
            return new[] { new Message
            {
                Type = MessageType.ERROR,
                Content = ToJson(ErrorResponse.AuthenticationErrorResponse("Not authenticated. Please login first."))
            } };
        }

        _logger.LogRequest(clientId, message.Content, user?.Username);

        var currentKb = _connectionManager.GetCurrentKb(clientId);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var parser = new KBMS.Parser.Parser(message.Content);
            var asts = parser.ParseAll();

            if (asts.Count == 0)
            {
                return new[] { 
                    new Message
                    {
                        Type = MessageType.RESULT,
                        Content = ToJson(new { message = "Empty query or comments ignored." })
                    },
                    new Message
                    {
                        Type = MessageType.FETCH_DONE,
                        Content = ToJson(new { statementsExecuted = 0, executionTime = 0.0 })
                    }
                };
            }

            int statementsExecuted = 0;
            foreach (var ast in asts)
            {
                try
                {
                    var result = _knowledgeManager.Execute(ast, user, currentKb);
                    
                    // (Phase 7) Tabular Unification: Stream ALL QueryResultSet results
                    if (result is QueryResultSet qrs && qrs.Success)
                    {
                        var columns = qrs.Columns.Count > 0 ? qrs.Columns : (qrs.Objects.Count > 0 ? qrs.Objects[0].Values.Keys.ToList() : new List<string>());
                        var widths = new Dictionary<string, int>();
                        int maxColWidthAllowed = Math.Max(15, 120 / (columns.Count > 0 ? columns.Count : 1));

                        foreach (var col in columns)
                        {
                            int maxWidth = col.Length;
                            foreach (var obj in qrs.Objects)
                            {
                                if (obj.Values.TryGetValue(col, out var val) && val != null)
                                {
                                    string strVal = val is System.Text.Json.JsonElement je ? je.ToString() : val.ToString() ?? "";
                                    maxWidth = Math.Max(maxWidth, strVal.Length);
                                }
                            }
                            widths[col] = Math.Min(maxWidth, maxColWidthAllowed);
                        }

                        // 1. Send METADATA (even if no rows)
                        var metadata = new { qrs.ConceptName, qrs.Count, Columns = columns, Widths = widths };
                        await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.METADATA, Content = ToJson(metadata) });

                        // 2. Send each ROW
                        foreach (var obj in qrs.Objects)
                        {
                            await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.ROW, Content = ToJson(obj.Values) });
                        }
                    }
                    else if (result != null)
                    {
                        await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.RESULT, Content = ToJson(result) });
                    }

                    if (ast is UseKbNode useNode)
                    {
                        _connectionManager.SetSessionKb(clientId, useNode.KbName);
                    }
                    statementsExecuted++;
                }
                catch (Exception ex)
                {
                    await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.ERROR, Content = ToJson(ErrorResponse.RuntimeErrorResponse(ex, ast.ToString() ?? "")) });
                    return Array.Empty<Message>(); // Stop executing further statements in this batch, and DO NOT send FETCH_DONE.
                }
            }

            stopwatch.Stop();
            var elapsedSec = stopwatch.ElapsedMilliseconds / 1000.0;

            // Send standard FETCH_DONE at the end of all results
            await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.FETCH_DONE, Content = ToJson(new { statementsExecuted, executionTime = elapsedSec }) });

            return Array.Empty<Message>();
        }
        catch (ParserException ex)
        {
            return new[] { new Message { Type = MessageType.ERROR, Content = ToJson(ErrorResponse.ParserErrorResponse(ex, message.Content)) } };
        }
        catch (Exception ex)
        {
            _logger.Error(clientId, $"HandleQuery error: {ex.Message}", ex);
            return new[] { new Message { Type = MessageType.ERROR, Content = ToJson(ErrorResponse.RuntimeErrorResponse(ex, message.Content)) } };
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
