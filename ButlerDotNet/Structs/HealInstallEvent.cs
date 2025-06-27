using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class HealInstallEvent
{
    [JsonPropertyName("totalCorrupted")] public required long TotalCorrupted { get; init; }
    [JsonPropertyName("appliedCaseFixes")] public required bool AppliedCaseFixes { get; init; }
}
