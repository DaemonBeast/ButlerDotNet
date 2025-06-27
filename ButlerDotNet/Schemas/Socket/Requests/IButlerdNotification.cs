using System.Text.Json;

namespace ButlerDotNet.Schemas.Socket.Requests;

public abstract class BaseSocketRequestHandler<T, TData, TResponse> : BaseSocketRequestHandler
    where T : BaseSocketRequestHandler<T, TData, TResponse>, ISocketRequestHandler
{
    public abstract void Handle(TData data);

    public override void UnsafeHandle(ReadOnlyMemory<char> json)
    {
        TData notification;
        try
        {
            notification = JsonSerializer.Deserialize<TData>(json.Span)!;
        }
        catch (Exception e)
        {
            throw new AggregateException($"Failed to deserialize notification of type \"{typeof(T)}\".", e);
        }

        Handle(notification);
    }
}

public abstract class BaseSocketRequestHandler
{
    public abstract string Method { get; }

    public abstract ButlerClient ButlerClient { get; }

    public abstract void UnsafeHandle(ReadOnlyMemory<char> json);
}

public interface ISocketRequestHandler
{
    public static abstract BaseSocketRequestHandler Create(ButlerClient butlerClient);
}
