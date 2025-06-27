using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ButlerDotNet.Schemas.Socket.Notifications;
using ButlerDotNet.Utilities.Extensions;
using Microsoft.Extensions.Logging;

namespace ButlerDotNet;

public partial class ButlerClient
{
    private Session? _globalSession;
    private string? _hostname;
    private int? _port;
    private string? _secret;

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Dictionary<long, TaskCompletionSource<ReadOnlyMemory<char>>> _socketRequests = new();

    private readonly Dictionary<string, BaseSocketNotificationHandler> _socketNotificationHandlers = new();

    public Task<Session> EstablishSessionAsync(
        Action<Session.SessionBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        if (_globalSession == null || !_globalSession.TcpClient.Connected)
        {
            throw new Exception("Cannot start socket session before butler client is initialized.");
        }

        if (_hostname == null || _port == null || _secret == null)
        {
            throw new Exception("At least one of the hostname, port and/or secret is `null`.");
        }

        return Session.ConnectAsync(_hostname, _port.Value, _secret, configure, Logger, cancellationToken);
    }

    internal void InitializeSocket(string hostname, int port, string secret)
    {
        _hostname = hostname;
        _port = port;
        _secret = secret;

        _ = Task
            .Run(async () =>
            {
                Logger.LogTrace("Establishing global session...");

                _globalSession = await Session.ConnectAsync(
                    hostname,
                    port,
                    secret,
                    b =>
                    {
                        b.HandleLogging();

                        b.OnNotification<Session.MetaFlowEstablished>(
                            "MetaFlowEstablished",
                            _ =>
                            {
                                b.Logger.LogDebug("Global session established.");
                                b.Logger.LogInformation("Butler client initialized successfully.");

                                _initializedTaskCompletionSource.SetResult();
                            });
                    },
                    Logger);

                _ = _globalSession.SendRequest("Meta.Flow");
            })
            .ContinueWith(t =>
            {
                if (t.Exception != null) _initializedTaskCompletionSource.SetException(t.Exception);
            });
    }
}

public class Session : IDisposable
{
    public Methods.Methods Methods { get; }

    public TcpClient TcpClient { get; } = new();

    public ILogger Logger { get; }

    private readonly Dictionary<string, Action<ReadOnlyMemory<char>>> _notificationHandlers = new();
    private readonly Dictionary<string, Action<ReadOnlyMemory<char>, object?>> _requestHandlers = new();
    private readonly Dictionary<long, Action<ReadOnlyMemory<char>, ResponseWithError.ErrorData?>> _responseHandlers = new();

    private readonly Random _random = new();

    private const byte NewlineByte = (byte) '\n';

    public static async Task<Session> ConnectAsync(
        string hostname,
        int port,
        string secret,
        Action<SessionBuilder> configure,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var builder = new SessionBuilder(logger);
        configure.Invoke(builder);
        var session = builder.Session;

        await session.TcpClient.ConnectAsync(hostname, port, cancellationToken);

        var localEndpoint = (IPEndPoint) session.TcpClient.Client.LocalEndPoint!;

        logger.LogDebug(
            "Connected to {Hostname}:{Port} from {LocalHostname}:{LocalPort}",
            hostname,
            port,
            localEndpoint.Address,
            localEndpoint.Port);

        _ = Task.Run(session.HandleSocketMessagesAsync, cancellationToken);

        var result = await session.SendRequest<AuthenticateData.ResultData>(
            "Meta.Authenticate",
            new AuthenticateData { Secret = secret },
            cancellationToken);

        if (!result.Ok) throw new Exception("Failed to authenticate socket.");

        logger.LogDebug("Socket authentication successful.");

        return session;
    }

    private Session(ILogger logger)
    {
        Logger = logger;

        Methods = new Methods.Methods(this);
    }

    public void SendNotification(string method, object? parameters = null)
        => Send(parameters == null
            ? new Notification { Method = method }
            : new NotificationWithParameters
            {
                Method = method,
                Parameters = parameters
            });

    public Task SendRequest(
        string method,
        object? parameters = null,
        CancellationToken cancellationToken = default)
        => SendRequest<object>(method, parameters, cancellationToken);

    public Task<TResponse> SendRequest<TResponse>(
        string method,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var taskCompletionSource = new TaskCompletionSource<TResponse>();
        var id = _random.NextInt64();
        _responseHandlers.Add(id, Wrapper);

        var request = new RequestWithParameters
            {
                Method = method,
                Parameters = parameters ?? new object(),
                Id = id
            };

        Send(request);

        return taskCompletionSource.Task
            .WaitAsync(cancellationToken)
            .ContinueWith(t =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _responseHandlers[id] = (_, _) => { };
                }

                return t.Result;
            }, CancellationToken.None);

        void Wrapper(ReadOnlyMemory<char> json, ResponseWithError.ErrorData? error = null)
        {
            if (error != null)
            {
                // TODO: Need to insert message somehow.
                /*taskCompletionSource.SetException(
                    ButlerdExceptions.TryGetExceptionForErrorCode(error.Code, out var exception)
                        ? exception
                        : new ButlerdExceptions.ButlerdException(error.Message));*/
                taskCompletionSource.SetException(new ButlerdExceptions.ButlerdException(error.Message));

                return;
            }

            TResponse response;
            try
            {
                response = JsonSerializer.Deserialize<TResponse>(json.Span)!;
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(
                    new AggregateException($"Failed to deserialize response of type \"{typeof(TResponse)}\".", e));

                return;
            }

            taskCompletionSource.SetResult(response);
        }
    }

    public void Dispose()
    {
        // TODO: Doesn't quite work.
        TcpClient.Close();
        GC.SuppressFinalize(this);
    }

    private void Send(object message)
    {
        var stream = TcpClient.GetStream();
        JsonSerializer.Serialize(stream, message, ButlerClient.SerializerOptions);
        stream.WriteByte(NewlineByte);
    }

    private async Task HandleSocketMessagesAsync()
    {
        Logger.LogTrace("Listening for socket messages...");

        await using var stream = TcpClient.GetStream();
        using var streamReader = new StreamReader(stream, Encoding.UTF8);

        while (true)
        {
            try
            {
                var message = await streamReader.ReadLineAsync();
                if (message == null) break; // TODO: ???

                HandleSocketMessage(message.AsMemory());
            }
            catch (Exception e)
            {
                Logger.LogError("{Error}", e);
            }
        }

        Logger.LogTrace("Socket message stream was closed.");
    }

    private void HandleSocketMessage(ReadOnlyMemory<char> json)
    {
        RpcType messageType;

        object? id = null;
        string? method = null;

        string? parameters = null;
        string? result = null;
        string? error = null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                Logger.LogError("Received `null` or `undefined` socket message.");
                return;
            }

            var hasIdElement = false;

            if (root.TryGetProperty("id", out var idElement))
            {
                hasIdElement = true;

                if (idElement.ValueKind == JsonValueKind.Null)
                {
                    Logger.LogWarning("Received socket message with a `null` ID.");
                }
                else
                {
                    if (idElement.ValueKind == JsonValueKind.Number &&
                        idElement.TryGetInt64(out var i))
                    {
                        id = i;
                    }
                    else if (idElement.ValueKind == JsonValueKind.String)
                    {
                        id = idElement.GetString();
                    }
                    else
                    {
                        Logger.LogError(
                            "Received socket message with an ID that is neither a 64-bit `integer` nor a `string`: \"{ID}\".",
                            idElement.ToString());

                        return;
                    }
                }
            }

            if (root.TryGetProperty("method", out var methodElement))
            {
                if (methodElement.ValueKind == JsonValueKind.Null)
                {
                    Logger.LogError("Received socket message with a `null` method.");
                    return;
                }

                if (methodElement.ValueKind != JsonValueKind.String)
                {
                    Logger.LogError(
                        "Received socket message with a non-`string` method \"{Method}\".",
                        methodElement.ToString());

                    return;
                }

                method = methodElement.GetString()!;
            }

            messageType = method == null
                ? hasIdElement
                    ? RpcType.Response
                    : RpcType.Unknown
                : hasIdElement
                    ? RpcType.Request
                    : RpcType.Notification;

            switch (messageType)
            {
                case RpcType.Unknown:
                    Logger.LogError("Received socket message without a `method` or `id` element.");
                    return;
                case RpcType.Request or RpcType.Notification:
                {
                    if (root.TryGetProperty("params", out var parametersElement) &&
                        parametersElement.ValueKind != JsonValueKind.Null)
                    {
                        if (parametersElement.ValueKind == JsonValueKind.Object)
                        {
                            parameters = parametersElement.GetRawText();
                        }
                        else
                        {
                            Logger.LogError("Received socket request/notification with non-object `params`.");
                            return;
                        }
                    }
                    else
                    {
                        // TODO: Is this a good idea? Or even correct?
                        parameters = "{}";
                    }

                    break;
                }
                case RpcType.Response:
                {
                    if (root.TryGetProperty("result", out var resultElement))
                    {
                        if (resultElement.ValueKind == JsonValueKind.Object)
                        {
                            result = resultElement.GetRawText();
                        }
                        else if (resultElement.ValueKind == JsonValueKind.Null)
                        {
                            // TODO: Is this a good idea? Or even correct?
                            result = "{}";
                        }
                        else
                        {
                            Logger.LogError("received socket response with non-object `result`.");
                            return;
                        }
                    }
                    else if (root.TryGetProperty("error", out var errorElement))
                    {
                        if (errorElement.ValueKind == JsonValueKind.Object)
                        {
                            error = errorElement.GetRawText();
                        }
                        else
                        {
                            Logger.LogError("received socket response with non-object `error`.");
                            return;
                        }
                    }
                    else
                    {
                        Logger.LogError("Received socket response without a `result` or `error` element.");
                        return;
                    }

                    break;
                }
            }
        }
        catch (JsonException e)
        {
            Logger.LogError(e, "Received socket message with invalid JSON.");
            return;
        }

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (messageType)
        {
            case RpcType.Request:
                HandleSocketRequest(parameters.AsMemory(), method!, id);
                break;
            case RpcType.Notification:
                HandleSocketNotification(parameters.AsMemory(), method!);
                break;
            case RpcType.Response when id == null:
                Logger.LogError("Received socket response with a `null` `id`.");
                return;
            case RpcType.Response when id is long numberId:
            {
                if (result != null)
                {
                    HandleSocketResponse(result.AsMemory(), numberId);
                }
                else
                {
                    ResponseWithError.ErrorData errorData;
                    try
                    {
                        errorData = JsonSerializer.Deserialize<ResponseWithError.ErrorData>(error!)!;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Failed to deserialize error from socket response.");
                        return;
                    }

                    HandleSocketResponse(error.AsMemory(), numberId, errorData);
                }
                break;
            }
            case RpcType.Response:
                Logger.LogError("Received socket response with a non-`long` `id`.");
                return;
        }
    }

    private void HandleSocketRequest(ReadOnlyMemory<char> json, string method, object? id)
    {
        if (!_requestHandlers.TryGetValue(method, out var handler))
        {
            Logger.LogError("Received socket request with unknown method \"{Method}\".", method);
            return;
        }

        try
        {
            handler.Invoke(json, id);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to handle socket request.");
        }
    }

    private void HandleSocketResponse(ReadOnlyMemory<char> json, long id, ResponseWithError.ErrorData? error = null)
    {
        if (!_responseHandlers.TryGetValue(id, out var handler))
        {
            Logger.LogError("Received socket response with unknown ID \"{ID}\".", id);
            return;
        }

        try
        {
            handler.Invoke(json, error);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to handle socket response.");
        }
    }

    private void HandleSocketNotification(ReadOnlyMemory<char> json, string method)
    {
        if (!_notificationHandlers.TryGetValue(method, out var handler))
        {
            Logger.LogError("Received socket notification with unknown method \"{Method}\".", method);
            return;
        }

        try
        {
            handler.Invoke(json);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to handle socket notification.");
        }
    }

    public class SessionBuilder
    {
        public ILogger Logger { get; }

        internal readonly Session Session;

        internal SessionBuilder(ILogger logger)
        {
            Logger = logger;

            Session = new Session(logger);
        }

        public SessionBuilder OnNotification<TData>(string method, Action<TData> handler)
        {
            if (Session._notificationHandlers.ContainsKey(method))
            {
                throw new ArgumentException(
                    "Duplicate method name when registering notification handler.",
                    nameof(method));
            }

            Session._notificationHandlers.Add(method, Wrapper);
            return this;

            void Wrapper(ReadOnlyMemory<char> json)
            {
                TData notification;
                try
                {
                    notification = JsonSerializer.Deserialize<TData>(json.Span)!;
                }
                catch (Exception e)
                {
                    throw new AggregateException($"Failed to deserialize notification of type \"{typeof(TData)}\".", e);
                }

                handler.Invoke(notification);
            }
        }

        public SessionBuilder OnRequest<TData, TResponse>(string method, Func<TData, Task<TResponse>> handler)
        {
            if (Session._requestHandlers.ContainsKey(method))
            {
                throw new ArgumentException("Duplicate method name when registering request handler.", nameof(method));
            }

            Session._requestHandlers.Add(method, Wrapper);
            return this;

            void Wrapper(ReadOnlyMemory<char> json, object? id)
            {
                TData request;
                try
                {
                    request = JsonSerializer.Deserialize<TData>(json.Span)!;
                }
                catch (Exception e)
                {
                    throw new AggregateException($"Failed to deserialize request of type \"{typeof(TData)}\".", e);
                }

                handler.Invoke(request).ContinueWith(t =>
                {
                    if (t.Exception is { InnerException: ButlerdExceptions.ButlerdException butlerdException })
                    {
                        // TODO: Handle JSON-RPC specific errors.
                        // if (exception is )
                    }
                    else
                    {
                        Session.Send(t.Result == null
                            ? new ResponseWithError
                            {
                                Error = Errors.FailedToProcessRequest,
                                Id = id
                            }
                            : new ResponseWithResult
                            {
                                Result = t.Result,
                                Id = id
                            });
                    }
                });
            }
        }
    }

    public class AuthenticateData
    {
        [JsonPropertyName("secret")] public required string Secret { get; init; }

        public class ResultData
        {
            [JsonPropertyName("ok")] public bool Ok { get; init; }
        }
    }

    public class MetaFlowEstablished
    {
        [JsonPropertyName("pid")] public required long Pid { get; init; }
    }

    public class NotificationWithParameters : Notification
    {
        [JsonPropertyName("params")]
        [JsonPropertyOrder(2)]
        public required object Parameters { get; init; }
    }

    public class Notification : Rpc
    {
        [JsonPropertyName("method")]
        [JsonPropertyOrder(1)]
        public required string Method { get; init; }
    }

    public class RequestWithParameters : Request
    {
        [JsonPropertyName("params")]
        [JsonPropertyOrder(2)]
        public required object Parameters { get; init; }
    }

    public class Request : Rpc
    {
        [JsonPropertyName("method")]
        [JsonPropertyOrder(1)]
        public required string Method { get; init; }

        [JsonPropertyName("id")]
        [JsonPropertyOrder(3)]
        public required long Id { get; init; }
    }

    public class ResponseWithResult : Rpc
    {
        [JsonPropertyName("result")] public required object Result { get; init; }
        [JsonPropertyName("id")] public required object? Id { get; init; }
    }

    public class ResponseWithError : Rpc
    {
        [JsonPropertyName("error")] public required ErrorData Error { get; init; }
        [JsonPropertyName("id")] public required object? Id { get; init; }

        public class ErrorData
        {
            [JsonPropertyName("code")] public required long Code { get; init; }
            [JsonPropertyName("message")] public required string Message { get; init; }
            // TODO: Add `data` property.
        }
    }

    public abstract class Rpc
    {
#pragma warning disable CA1822
        [JsonPropertyName("jsonrpc")]
        [JsonPropertyOrder(0)]
        public string JsonRpc => "2.0";
#pragma warning restore CA1822
    }

    public static class Errors
    {
        public static ResponseWithError.ErrorData CannotRemoveInstallLocation { get; } = new()
        {
            Code = 18000,
            Message = "An install location could not be removed because it has active downloads"
        };

        public static ResponseWithError.ErrorData DatabaseBusy { get; } = new()
        {
            Code = 16000,
            Message = "The database is busy"
        };

        public static ResponseWithError.ErrorData ApiError { get; } = new()
        {
            Code = 12000,
            Message = "API error"
        };

        public static ResponseWithError.ErrorData NoInternetConnection { get; } = new()
        {
            Code = 9000,
            Message = "There is no Internet connection"
        };

        public static ResponseWithError.ErrorData JavaNotFound { get; } = new()
        {
            Code = 6000,
            Message = "Java Runtime Environment is required to launch this title."
        };

        public static ResponseWithError.ErrorData NothingToLaunch { get; } = new()
        {
            Code = 5000,
            Message = "Nothing that can be launched was found"
        };

        public static ResponseWithError.ErrorData TitleHostedOnIncompatibleThirdPartySite { get; } = new()
        {
            Code = 3001,
            Message = "This title is hosted on an incompatible third-party website"
        };

        public static ResponseWithError.ErrorData NoCompatibleUploadToInstall { get; } = new()
        {
            Code = 2001,
            Message = "We tried to install something, but could not find compatible uploads"
        };

        public static ResponseWithError.ErrorData OperationCancelledGracefully { get; } = new()
        {
            Code = 499,
            Message = "An operation was cancelled gracefully"
        };

        public static ResponseWithError.ErrorData OperationAbortedByUser { get; } = new()
        {
            Code = 410,
            Message = "An operation was aborted by the user"
        };

        public static ResponseWithError.ErrorData InstallFolderMissingDuringLaunch { get; } = new()
        {
            Code = 404,
            Message = "We tried to launch something, but the install folder just wasn’t there"
        };

        public static ResponseWithError.ErrorData FailedToProcessRequest { get; } = new()
        {
            Code = -32000,
            Message = "Failed to process request."
        };

        public static ResponseWithError.ErrorData InvalidRequest { get; } = new()
        {
            Code = -32600,
            Message = "Invalid Request"
        };

        public static ResponseWithError.ErrorData MethodNotFound { get; } = new()
        {
            Code = -32601,
            Message = "Method not found"
        };

        public static ResponseWithError.ErrorData InvalidParameters { get; } = new()
        {
            Code = -32602,
            Message = "Invalid params"
        };

        public static ResponseWithError.ErrorData InternalError { get; } = new()
        {
            Code = -32603,
            Message = "Internal error"
        };
    }

    public static class ButlerdExceptions
    {
        public static bool TryGetExceptionForErrorCode(
            long errorCode,
            [NotNullWhen(true)] out ButlerdException? exception)
        {
            exception = errorCode switch
            {
                18000 => new CannotRemoveInstallLocationException(),
                16000 => new DatabaseBusyException(),
                12000 => new ApiErrorException(),
                9000 => new NoInternetConnectionException(),
                6000 => new JavaNotFoundException(),
                5000 => new NothingToLaunchException(),
                3001 => new TitleHostedOnIncompatibleThirdPartySiteException(),
                2001 => new NoCompatibleUploadToInstallException(),
                499 => new OperationCancelledGracefullyException(),
                410 => new OperationAbortedByUserException(),
                404 => new InstallFolderMissingDuringLaunchException(),
                -32000 => new FailedToProcessRequestException(),
                -326000 => new InvalidRequestException(),
                -32601 => new MethodNotFoundException(),
                -32602 => new InvalidParametersException(),
                -32603 => new InternalErrorException(),
                _ => null
            };

            return exception != null;
        }

        public class CannotRemoveInstallLocationException() : ButlerdException(Errors.CannotRemoveInstallLocation.Message);
        public class DatabaseBusyException() : ButlerdException(Errors.DatabaseBusy.Message);
        public class ApiErrorException() : ButlerdException(Errors.ApiError.Message);
        public class NoInternetConnectionException() : ButlerdException(Errors.NoInternetConnection.Message);
        public class JavaNotFoundException() : ButlerdException(Errors.JavaNotFound.Message);
        public class NothingToLaunchException() : ButlerdException(Errors.NothingToLaunch.Message);
        public class TitleHostedOnIncompatibleThirdPartySiteException() : ButlerdException(Errors.TitleHostedOnIncompatibleThirdPartySite.Message);
        public class NoCompatibleUploadToInstallException() : ButlerdException(Errors.NoCompatibleUploadToInstall.Message);
        public class OperationCancelledGracefullyException() : ButlerdException(Errors.OperationCancelledGracefully.Message);
        public class OperationAbortedByUserException() : ButlerdException(Errors.OperationAbortedByUser.Message);
        public class InstallFolderMissingDuringLaunchException() : ButlerdException(Errors.InstallFolderMissingDuringLaunch.Message);
        public class FailedToProcessRequestException() : ButlerdException(Errors.FailedToProcessRequest.Message);
        public class InvalidRequestException() : ButlerdException(Errors.InvalidRequest.Message);
        public class MethodNotFoundException() : ButlerdException(Errors.MethodNotFound.Message);
        public class InvalidParametersException() : ButlerdException(Errors.InvalidParameters.Message);
        public class InternalErrorException() : ButlerdException(Errors.InternalError.Message);
        public class ButlerdException(string message) : Exception(message);
    }

    public enum RpcType
    {
        Unknown,
        Request,
        Response,
        Notification
    }
}
