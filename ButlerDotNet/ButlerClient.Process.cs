using System.ComponentModel;
using System.Text.Json;
using ButlerDotNet.Schemas.Butlerd;
using Microsoft.Extensions.Logging;

namespace ButlerDotNet;

public partial class ButlerClient
{
    private readonly Dictionary<string, BaseButlerdNotificationHandler> _butlerdNotificationHandlers = new();

    public void RegisterButlerdNotificationHandler<TButlerdNotificationHandler, TData>()
        where TButlerdNotificationHandler : BaseButlerdNotificationHandler<TButlerdNotificationHandler, TData>, IButlerdNotificationHandler
    {
        var handler = TButlerdNotificationHandler.Create(this);
        if (!_butlerdNotificationHandlers.TryAdd(handler.Type, handler))
        {
            Logger.LogError(
                "Attempted to register duplicate butlerd notification handler for type \"{Type}\".",
                handler.Type);
        }
    }

    private void StartButlerProcess()
    {
        try
        {
            _butlerProcess.Start();
        }
        catch (Win32Exception e)
        {
            throw new AggregateException("Unable to start a butler process. butler must be installed.", e);
        }
    }

    private async Task HandleButlerdNotificationsAsync()
    {
        Logger.LogTrace("Listening for butlerd notifications...");

        while (true)
        {
            var json = await _butlerProcess.StandardOutput.ReadLineAsync();
            if (json == null) break;

            HandleButlerdNotification(json.AsMemory());
        }

        Logger.LogTrace("butlerd notification stream was closed.");
    }

    private void HandleButlerdNotification(ReadOnlyMemory<char> json)
    {
        string type;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                Logger.LogError("Received `null` or `undefined` butlerd notification.");
                return;
            }

            if (!root.TryGetProperty("type", out var typeElement))
            {
                Logger.LogError("Received butlerd notification without a type element.");
                return;
            }

            if (typeElement.ValueKind == JsonValueKind.Null)
            {
                Logger.LogError("Received butlerd notification with a `null` type.");
                return;
            }

            if (typeElement.ValueKind != JsonValueKind.String)
            {
                Logger.LogError(
                    "Received butlerd notification with a non-string type \"{Type}\".",
                    typeElement.ToString());

                return;
            }

            type = typeElement.GetString()!;
        }
        catch (JsonException)
        {
            Logger.LogDebug("{Notification}", json);
            return;
        }

        if (!_butlerdNotificationHandlers.TryGetValue(type, out var handler))
        {
            Logger.LogError("Received butlerd notification with unknown type \"{Type}\".", type);
            return;
        }

        try
        {
            handler.UnsafeHandle(json);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to handle butlerd notification.");
        }
    }
}
