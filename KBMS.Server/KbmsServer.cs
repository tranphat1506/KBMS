using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
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
    private readonly KBMS.Storage.V3.DiskManager _diskManager;
    private readonly KBMS.Storage.V3.BufferPoolManager _bpm;
    private readonly KBMS.Storage.V3.KbCatalog _kbCatalog;
    private readonly KBMS.Storage.V3.ConceptCatalog _conceptCatalog;
    private readonly KBMS.Storage.V3.UserCatalog _userCatalog;
    private readonly KBMS.Storage.V3.WalManagerV3 _wal;
    private readonly KBMS.Server.V3.SystemKbBootstrapper _bootstrapper;
    private readonly KBMS.Server.V3.SystemLogger _sysLogger;
    private readonly KBMS.Server.V3.ManagementManager _managementManager;
    private bool _isRunning;
    private TcpListener? _listener;
    private readonly CancellationTokenSource _cts = new();

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public KbmsServer() : this("127.0.0.1", 3307, "data")
    {
    }

    public KbmsServer(string host, int port) : this(host, port, "data")
    {
    }

    public KbmsServer(string host, int port, string dataDir)
    {
        _host = host;
        _port = port;
        _connectionManager = new ConnectionManager();
        
        // V3 Infrastructure Setup
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        string dbFile = Path.Combine(dataDir, "v3_database.kdb");
        
        _diskManager = new KBMS.Storage.V3.DiskManager(dbFile);
        _bpm = new KBMS.Storage.V3.BufferPoolManager(_diskManager, 256);
        _wal = new KBMS.Storage.V3.WalManagerV3(dbFile);
        
        _kbCatalog = new KBMS.Storage.V3.KbCatalog(_bpm, _diskManager);
        _conceptCatalog = new KBMS.Storage.V3.ConceptCatalog(_bpm, _diskManager);
        _userCatalog = new KBMS.Storage.V3.UserCatalog(_bpm, _diskManager);
        
        _authManager = new AuthenticationManager(_userCatalog);
        _knowledgeManager = new KnowledgeManager(_bpm, _diskManager, _kbCatalog, _conceptCatalog, _userCatalog, _wal);
        
        // V3 System KB & Logging
        var v3Router = _knowledgeManager.V3Router;
        _sysLogger = new KBMS.Server.V3.SystemLogger(v3Router);
        _bootstrapper = new KBMS.Server.V3.SystemKbBootstrapper(_kbCatalog, _conceptCatalog, v3Router);
        _bootstrapper.Bootstrap();


        _managementManager = new KBMS.Server.V3.ManagementManager(_connectionManager, _sysLogger);
        
        _isRunning = false;

        // Initialize system users if empty
        if (!_userCatalog.ListUsers().Any())
        {
            _userCatalog.CreateUser("root", "root", UserRole.ROOT);
        }
    }

    public async Task StartAsync()
    {
        var ipAddress = ResolveHost(_host);
        _listener = new TcpListener(ipAddress, _port);
        _listener.Start();
        _isRunning = true;

        _sysLogger.Info("System", $"KBMS Server started on {_host}:{_port}");

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
                        _sysLogger.Error("System", $"Accept error: {ex.Message}");
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
        _connectionManager.CloseAll();
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var clientId = GenerateClientId();
        var ip = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        var session = _connectionManager.CreateSession(clientId, client, ip);
        
        _sysLogger.Info(clientId, "New connection established");

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
            _sysLogger.Error(clientId, $"Client error: {ex.Message}");
        }
        finally
        {
            _connectionManager.RemoveSession(clientId);
            _sysLogger.Info(clientId, "Connection closed");
        }
    }

    private async Task ProcessMessageAsync(Message message, string clientId, Stream stream)
    {
        switch (message.Type)
        {
            case MessageType.LOGIN:
                _sysLogger.Info(clientId, "REQUEST: LOGIN");
                var loginResponse = HandleLogin(message, clientId);
                await SendProtocolMessageAsync(clientId, stream, loginResponse);
                _sysLogger.LogResponse(clientId, loginResponse.Type.ToString(), loginResponse.Content);
                break;
            case MessageType.QUERY:
                _sysLogger.LogRequest(clientId, message.Content, _connectionManager.GetCurrentUser(clientId)?.Username);
                await HandleQueryAsync(message, clientId, stream);
                break;
            case MessageType.LOGOUT:
                _sysLogger.Info(clientId, "REQUEST: LOGOUT");
                var logoutResponse = HandleLogout(message, clientId);
                await SendProtocolMessageAsync(clientId, stream, logoutResponse);
                break;
            case MessageType.STATS:
                await HandleManagementRequestAsync(message, clientId, stream, () => _managementManager.GetSystemStats());
                break;
            case MessageType.SESSIONS:
                await HandleManagementRequestAsync(message, clientId, stream, () => _managementManager.ListSessions());
                break;
            case MessageType.LOGS_STREAM:
                HandleLogsStream(clientId, stream);
                break;
            case MessageType.MANAGEMENT_CMD:
                await HandleManagementCommandAsync(message, clientId, stream);
                break;
            default:
                await SendProtocolMessageAsync(clientId, stream, new Message { Type = MessageType.ERROR, Content = "Unknown message type" });
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
            
            _sysLogger.LogAudit(user.Username, "LOGIN", "SUCCESS", clientId);

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
        
        // Use incoming RequestId if present, otherwise generate one
        var requestId = string.IsNullOrEmpty(message.RequestId) 
            ? $"req_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N")[..6]}"
            : message.RequestId;

        try
        {
            var user = _connectionManager.GetCurrentUser(clientId);
            if (user == null)
            {
                await SendProtocolMessageAsync(clientId, stream, new Message 
                { 
                    Type = MessageType.ERROR, 
                    RequestId = requestId,
                    Content = ToJson(new { error = "Not logged in" }) 
                });
                return; 
            }

            var currentKb = _connectionManager.GetCurrentKb(clientId);
            var parser = new KBMS.Parser.Parser(message.Content);
            var asts = parser.ParseAll();

            if (!asts.Any())
            {
                await SendProtocolMessageAsync(clientId, stream, new Message 
                { 
                    Type = MessageType.ERROR, 
                    RequestId = requestId,
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
                    
                    _sysLogger.LogAudit(user.Username, ast.ToString() ?? "QUERY", "SUCCESS", clientId);
                    
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
                        await SendProtocolMessageAsync(clientId, stream, new Message 
                        { 
                            Type = MessageType.METADATA, 
                            RequestId = requestId,
                            Content = ToJson(metadata) 
                        });

                        var batch = new List<Dictionary<string, object?>>();
                        foreach (var obj in qrs.Objects)
                        {
                            batch.Add(obj.Values);
                            if (batch.Count >= 100)
                            {
                                await SendProtocolMessageAsync(clientId, stream, new Message 
                                { 
                                    Type = MessageType.ROW, 
                                    RequestId = requestId,
                                    Content = ToJson(batch) 
                                });
                                batch.Clear();
                            }
                        }
                        
                        if (batch.Count > 0)
                        {
                            await SendProtocolMessageAsync(clientId, stream, new Message 
                            { 
                                Type = MessageType.ROW, 
                                RequestId = requestId,
                                Content = ToJson(batch) 
                            });
                        }
                    }
                    else if (result is ErrorResponse err)
                    {
                        await SendProtocolMessageAsync(clientId, stream, new Message 
                        { 
                            Type = MessageType.ERROR, 
                            RequestId = requestId,
                            Content = ToJson(err) 
                        });
                    }
                    else if (result != null)
                    {
                        await SendProtocolMessageAsync(clientId, stream, new Message 
                        { 
                            Type = MessageType.RESULT, 
                            RequestId = requestId,
                            Content = ToJson(result) 
                        });
                    }

                    if (ast is UseKbNode useNode)
                    {
                        _connectionManager.SetSessionKb(clientId, useNode.KbName);
                    }
                    statementsExecuted++;
                }
                catch (Exception ex)
                {
                    await SendProtocolMessageAsync(clientId, stream, new Message 
                    { 
                        Type = MessageType.ERROR, 
                        RequestId = requestId,
                        Content = ToJson(ErrorResponse.RuntimeErrorResponse(ex, ast.ToString() ?? "")) 
                    });
                    break; 
                }
            }
        }
        catch (ParserException ex)
        {
            await SendProtocolMessageAsync(clientId, stream, new Message 
            { 
                Type = MessageType.ERROR, 
                RequestId = requestId,
                Content = ToJson(ErrorResponse.ParserErrorResponse(ex.Message, message.Content, ex.Line, ex.Column)) 
            });
        }
        catch (Exception ex)
        {
            _sysLogger.Error(clientId, $"HandleQuery error: {ex.Message}");
            await SendProtocolMessageAsync(clientId, stream, new Message 
            { 
                Type = MessageType.ERROR, 
                RequestId = requestId,
                Content = ToJson(ErrorResponse.RuntimeErrorResponse(ex, message.Content)) 
            });
        }
        finally
        {
            stopwatch.Stop();
            var elapsedSec = stopwatch.ElapsedMilliseconds / 1000.0;
            var fetchDoneJson = ToJson(new { statementsExecuted, executionTime = elapsedSec });
            await SendProtocolMessageAsync(clientId, stream, new Message 
            { 
                Type = MessageType.FETCH_DONE, 
                RequestId = requestId,
                Content = fetchDoneJson 
            });
            _sysLogger.Info(clientId, $"RESPONSE: FETCH_DONE ({fetchDoneJson}) [Req: {requestId}]");
        }
    }

    private Message HandleLogout(Message message, string clientId)
    {
        _connectionManager.SetSessionUser(clientId, null);
        return new Message { Type = MessageType.RESULT, Content = "LOGOUT_SUCCESS" };
    }

    private async Task HandleManagementRequestAsync(Message message, string clientId, Stream stream, Func<object> action)
    {
        var user = _connectionManager.GetCurrentUser(clientId);
        if (user == null || user.Role != UserRole.ROOT)
        {
            await SendProtocolMessageAsync(clientId, stream, new Message { Type = MessageType.ERROR, Content = "Unauthorized: Root access required" });
            return;
        }

        try
        {
            var result = action();
            await SendProtocolMessageAsync(clientId, stream, new Message
            {
                Type = MessageType.RESULT,
                RequestId = message.RequestId,
                Content = ToJson(result)
            });

            // Send FETCH_DONE to signal completion of the management request
            await SendProtocolMessageAsync(clientId, stream, new Message
            {
                Type = MessageType.FETCH_DONE,
                RequestId = message.RequestId,
                Content = "{}"
            });
        }
        catch (Exception ex)
        {
            await SendProtocolMessageAsync(clientId, stream, new Message { Type = MessageType.ERROR, Content = ex.Message });
        }
    }

    private void HandleLogsStream(string clientId, Stream stream)
    {
        var user = _connectionManager.GetCurrentUser(clientId);
        if (user == null || user.Role != UserRole.ROOT)
        {
            // We can't easily send an error message and keep the stream open if the protocol doesn't support it well,
            // but for now let's just ignore or send a one-time error.
            return;
        }

        _managementManager.SubscribeToLogs(clientId, stream);
        _sysLogger.Info(clientId, "Subscribed to real-time logs");
    }

    private async Task HandleManagementCommandAsync(Message message, string clientId, Stream stream)
    {
        var user = _connectionManager.GetCurrentUser(clientId);
        if (user == null || user.Role != UserRole.ROOT)
        {
            await SendProtocolMessageAsync(clientId, stream, new Message { Type = MessageType.ERROR, Content = "Unauthorized: Root access required" });
            return;
        }

        try
        {
            var content = message.Content;
            if (content.StartsWith("KILL_SESSION "))
            {
                var sessionId = content.Substring("KILL_SESSION ".Length).Trim();
                if (_connectionManager.KillSession(sessionId))
                {
                    await SendProtocolMessageAsync(clientId, stream, new Message { Type = MessageType.RESULT, RequestId = message.RequestId, Content = ToJson(new { success = true, message = $"Session {sessionId} terminated" }) });
                }
                else
                {
                    await SendProtocolMessageAsync(clientId, stream, new Message { Type = MessageType.ERROR, RequestId = message.RequestId, Content = "Session not found" });
                }
            }
            else
            {
                await SendProtocolMessageAsync(clientId, stream, new Message { Type = MessageType.ERROR, Content = "Unknown management command" });
            }
        }
        catch (Exception ex)
        {
            await SendProtocolMessageAsync(clientId, stream, new Message { Type = MessageType.ERROR, Content = ex.Message });
        }
    }

    private async Task SendProtocolMessageAsync(string clientId, Stream stream, Message message)
    {
        var session = _connectionManager.GetSession(clientId);
        if (session != null)
        {
            await session.MessageLock.WaitAsync();
            try
            {
                await Protocol.SendMessageAsync(stream, message);
            }
            finally
            {
                session.MessageLock.Release();
            }
        }
        else
        {
            await Protocol.SendMessageAsync(stream, message);
        }
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
