using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace ButlerDotNet.Utilities.Extensions;

public static class SessionBuilderExtensions
{
    public static Session.SessionBuilder HandleLogging(this Session.SessionBuilder builder)
        => builder.OnNotification<LogNotification>(
            "Log",
            h => builder.Logger.Log(h.GetLogLevel(), "{Message}", h.Message));

    public static Session.SessionBuilder HandleProgress(this Session.SessionBuilder builder)
        => builder.OnNotification<ProgressNotification>(
            "Progress",
            h =>
            {
                var progress = $"{h.Progress * 100:.00}%".PadRight(15);
                var timeRemaining = TimeSpan.FromSeconds(h.EstimatedSecondsRemaining).FormatAsMinutes().PadRight(15);
                var speed = $"{UnitUtilities.CompressBytes(h.BytesPerSecond, out var unit):.00} {unit}/s".PadRight(15);

                builder.Logger.LogDebug(
                    "Progress: {Progress}ETA: {TimeRemaining}Speed: {Speed}", progress, timeRemaining, speed);
            });

    public class LogNotification
    {
        [JsonPropertyName("level")] public required string Level { get; init; }
        [JsonPropertyName("message")] public required string Message { get; init; }

        public LogLevel GetLogLevel()
            => Level switch
            {
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Information,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                _ => LogLevel.Trace
            };
    }

    public class ProgressNotification
    {
        [JsonPropertyName("progress")] public required double Progress { get; init; }
        [JsonPropertyName("eta")] public required double EstimatedSecondsRemaining { get; init; }
        [JsonPropertyName("bps")] public required double BytesPerSecond { get; init; }
    }
}
