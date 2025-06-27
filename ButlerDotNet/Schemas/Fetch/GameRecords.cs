using System.Text.Json.Serialization;

namespace ButlerDotNet.Schemas.Fetch;

public class GameRecords
{
    [JsonPropertyName("profileId")] public required long ProfileId { get; init; }
    [JsonPropertyName("source")] public required string Source { get; init; }
    // [JsonPropertyName("collectionId")] public long
}
