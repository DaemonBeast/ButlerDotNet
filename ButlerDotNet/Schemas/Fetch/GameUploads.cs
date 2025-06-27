using System.Text.Json.Serialization;
using ButlerDotNet.Structs;

namespace ButlerDotNet.Schemas.Fetch;

public class GameUploads
{
    [JsonPropertyName("gameId")] public required long GameId { get; init; }
    [JsonPropertyName("compatible")] public required bool Compatible { get; init; }
    [JsonPropertyName("fresh")] public bool? Fresh { get; init; }

    public class Result
    {
        [JsonPropertyName("uploads")] public required Upload[] Uploads { get; init; }
        [JsonPropertyName("stale")] public bool? Stale { get; init; }
    }
}
