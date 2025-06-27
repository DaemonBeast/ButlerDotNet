using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class PatchingInstallEvent
{
    [JsonPropertyName("buildID")] public required long BuildId { get; init; }
    [JsonPropertyName("subtype")] public required string Subtype { get; init; }
}
