using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace ButlerDotNet.Schemas.Butlerd;

public class LogNotificationHandler(ButlerClient butlerClient) :
    BaseButlerdNotificationHandler<LogNotificationHandler, LogNotification>,
    IButlerdNotificationHandler
{
    public override string Type => "log";

    public override ButlerClient ButlerClient { get; } = butlerClient;

    public static BaseButlerdNotificationHandler Create(ButlerClient butlerClient)
        => new LogNotificationHandler(butlerClient);

    public override void Handle(LogNotification logNotification)
        => ButlerClient.Logger.Log(logNotification.GetLogLevel(), "{Message}", logNotification.Message);
}

public class LogNotification
{
    [JsonPropertyName("level")] public required string Level { get; init; }
    [JsonPropertyName("message")] public required string Message { get; init; }
    [JsonPropertyName("time")] public required long Time { get; init; }

    public LogLevel GetLogLevel()
        => Level switch
        {
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            _ => LogLevel.Trace
        };

    public DateTimeOffset GetTime()
        => DateTimeOffset.FromUnixTimeSeconds(Time);
}
