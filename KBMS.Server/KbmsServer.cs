using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KBMS.Models;
using KBMS.Network;
using KBMS.Parser;
using KBMS.Parser.Ast;
using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kql;
using KBMS.Storage;
using KBMS.Knowledge;

namespace KBMS.Server;

public class KbmsServer
{
    private readonly int _port;
    private readonly string _host;
    private readonly ConnectionManager _connectionManager;
    private readonly AuthenticationManager _authManager;
    private readonly KnowledgeManager _knowledgeManager;
    private readonly Logger _logger;
    private bool _isRunning;
    private TcpListener? _listener;
    private readonly CancellationTokenSource _cts = new();

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public KbmsServer() : this("127.0.0.1", 3307, new StorageEngine("data", "kbms_encryption_key"))
    {
    }

    public KbmsServer(string host, int port, StorageEngine storage)
    {
        _host = host;
        _port = port;
        _logger = new Logger();
        _connectionManager = new ConnectionManager();
        _authManager = new AuthenticationManager(storage);
        _knowledgeManager = new KnowledgeManager(storage);
        _isRunning = false;
    }

    public async Task StartAsync()
    {
        var ipAddress = ResolveHost(_host);
        _listener = new TcpListener(ipAddress, _port);
        _listener.Start();
        _isRunning = true;

        _logger.Info("System", $"KBMS Server started on {_host}:{_port}");

        try
        {
            while (_isRunning && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Use AcceptTcpClientAsync with cancellation where possible, 
                    // or rely on listener.Stop() to break the block.
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = HandleClientAsync(client);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException) when (!_isRunning)
                {
                    break; 
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        _logger.Error("System", $"Accept error: {ex.Message}");
                }
            }
        }
        finally
        {
            _listener.Stop();
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _cts.Cancel();
        _listener?.Stop();
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var clientId = GenerateClientId();
        _connectionManager.CreateSession(clientId);
        _logger.Info(clientId, "New connection established");

        using var stream = client.GetStream();
        try
        {
            while (client.Connected)
            {
                var message = await Protocol.ReceiveMessageAsync(stream);
                if (message == null) break;

                await ProcessMessageAsync(message, clientId, stream);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(clientId, $"Client error: {ex.Message}");
        }
        finally
        {
            _connectionManager.RemoveSession(clientId);
            _logger.Info(clientId, "Connection closed");
        }
    }

    private async Task ProcessMessageAsync(Message message, string clientId, Stream stream)
    {
        switch (message.Type)
        {
            case MessageType.LOGIN:
                _logger.Info(clientId, "REQUEST: LOGIN");
                var loginResponse = HandleLogin(message, clientId);
                await Protocol.SendMessageAsync(stream, loginResponse);
                _logger.Info(clientId, $"RESPONSE: {loginResponse.Type} (Content: {loginResponse.Content})");
                break;
            case MessageType.QUERY:
                _logger.LogRequest(clientId, message.Content, _connectionManager.GetCurrentUser(clientId)?.Username);
                await HandleQueryAsync(message, clientId, stream);
                break;
            case MessageType.LOGOUT:
                _logger.Info(clientId, "REQUEST: LOGOUT");
                var logoutResponse = HandleLogout(message, clientId);
                await Protocol.SendMessageAsync(stream, logoutResponse);
                break;
            default:
                await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.ERROR, Content = "Unknown message type" });
                break;
        }
    }

    private Message HandleLogin(Message message, string clientId)
    {
        try
        {
            var content = message.Content;
            if (string.IsNullOrEmpty(content) || !content.StartsWith("LOGIN "))
            {
                return new Message { Type = MessageType.ERROR, Content = "Invalid login format. Use: LOGIN user password" };
            }

            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                return new Message { Type = MessageType.ERROR, Content = "Missing username or password" };
            }

            var username = parts[1];
            var password = parts[2];

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
        catch (Exception ex)
        {
            return new Message { Type = MessageType.ERROR, Content = $"Login error: {ex.Message}" };
        }
    }

    private async Task HandleQueryAsync(Message message, string clientId, Stream stream)
    {
        var stopwatch = Stopwatch.StartNew();
        int statementsExecuted = 0;
        
        try
        {
            var user = _connectionManager.GetCurrentUser(clientId);
            if (user == null)
            {
                await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.ERROR, Content = ToJson(new { error = "Not logged in" }) });
                return; 
            }

            var currentKb = _connectionManager.GetCurrentKb(clientId);
            var parser = new KBMS.Parser.Parser(message.Content);
            var asts = parser.ParseAll();

            if (!asts.Any())
            {
                await Protocol.SendMessageAsync(stream, new Message 
                { 
                    Type = MessageType.RESULT, 
                    Content = ToJson(new { message = "Empty query or comments ignored." }) 
                });
                return;
            }

            foreach (var ast in asts)
            {
                try
                {
                    // Refresh currentKb from session in case it was updated by a previous statement (e.g., USE)
                    currentKb = _connectionManager.GetCurrentKb(clientId);
                    var result = _knowledgeManager.Execute(ast, user, currentKb);
                    
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

                        var metadata = new { qrs.ConceptName, qrs.Count, Columns = columns, Widths = widths };
                        await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.METADATA, Content = ToJson(metadata) });

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
                    break; 
                }
            }
        }
        catch (ParserException ex)
        {
            await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.ERROR, Content = ToJson(ErrorResponse.ParserErrorResponse(ex, message.Content)) });
        }
        catch (Exception ex)
        {
            _logger.Error(clientId, $"HandleQuery error: {ex.Message}", ex);
            await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.ERROR, Content = ToJson(ErrorResponse.RuntimeErrorResponse(ex, message.Content)) });
        }
        finally
        {
            stopwatch.Stop();
            var elapsedSec = stopwatch.ElapsedMilliseconds / 1000.0;
            var fetchDoneJson = ToJson(new { statementsExecuted, executionTime = elapsedSec });
            await Protocol.SendMessageAsync(stream, new Message { Type = MessageType.FETCH_DONE, Content = fetchDoneJson });
            _logger.Info(clientId, $"RESPONSE: FETCH_DONE ({fetchDoneJson})");
        }
    }

    private Message HandleLogout(Message message, string clientId)
    {
        _connectionManager.SetSessionUser(clientId, null);
        return new Message { Type = MessageType.RESULT, Content = "LOGOUT_SUCCESS" };
    }

    private string ToJson(object obj) => JsonSerializer.Serialize(obj, _jsonOptions);

    private string GenerateClientId()
    {
        return $"conn_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N")[..8]}";
    }

    private IPAddress ResolveHost(string host)
    {
        if (host == "localhost" || host == "127.0.0.1") return IPAddress.Loopback;
        if (host == "0.0.0.0") return IPAddress.Any;
        if (IPAddress.TryParse(host, out var ip)) return ip;
        var addresses = System.Net.Dns.GetHostAddresses(host);
        return addresses.FirstOrDefault() ?? IPAddress.Any;
    }
}
