using System.Text.Json;

namespace ButlerDotNet.Schemas.Butlerd;

public abstract class BaseButlerdNotificationHandler<T, TData> : BaseButlerdNotificationHandler
    where T : BaseButlerdNotificationHandler<T, TData>, IButlerdNotificationHandler
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

public abstract class BaseButlerdNotificationHandler
{
    public abstract string Type { get; }

    public abstract ButlerClient ButlerClient { get; }

    public abstract void UnsafeHandle(ReadOnlyMemory<char> json);
}

public interface IButlerdNotificationHandler
{
    public static abstract BaseButlerdNotificationHandler Create(ButlerClient butlerClient);
}
