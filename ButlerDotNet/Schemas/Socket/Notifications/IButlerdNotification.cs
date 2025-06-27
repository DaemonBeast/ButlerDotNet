using System.Text.Json;

namespace ButlerDotNet.Schemas.Socket.Notifications;

public abstract class BaseSocketNotificationHandler<T, TData> : BaseSocketNotificationHandler
    where T : BaseSocketNotificationHandler<T, TData>, ISocketNotificationHandler
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

public abstract class BaseSocketNotificationHandler
{
    public abstract string Method { get; }

    public abstract ButlerClient ButlerClient { get; }

    public abstract void UnsafeHandle(ReadOnlyMemory<char> json);
}

public interface ISocketNotificationHandler
{
    public static abstract BaseSocketNotificationHandler Create(ButlerClient butlerClient);
}
