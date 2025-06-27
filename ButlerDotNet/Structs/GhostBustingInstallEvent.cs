using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class GhostBustingInstallEvent
{
    [JsonPropertyName("operation")] public required string Operation { get; init; }
    [JsonPropertyName("found")] public required long Found { get; init; }
    [JsonPropertyName("removed")] public required long Removed { get; init; }
}
