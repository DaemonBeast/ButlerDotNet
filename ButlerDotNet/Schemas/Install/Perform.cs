using System.Text.Json.Serialization;
using ButlerDotNet.Structs;

namespace ButlerDotNet.Schemas.Install;

public class Perform
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("stagingFolder")] public required string StagingFolder { get; init; }

    public class Result
    {
        [JsonPropertyName("caveId")] public required string CaveId { get; init; }
        [JsonPropertyName("events")] public required InstallEvent[] Events { get; init; }
    }
}
