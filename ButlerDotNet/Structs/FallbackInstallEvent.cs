using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class FallbackInstallEvent
{
    [JsonPropertyName("attempted")] public required string Attempted { get; init; }
    [JsonPropertyName("problem")] public required ProblemInstallEvent Problem { get; init; }
    [JsonPropertyName("nowTrying")] public required string NowTrying { get; init; }
}
