using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class ProblemInstallEvent
{
    [JsonPropertyName("error")] public required string Error { get; init; }
    [JsonPropertyName("errorStack")] public required string ErrorStack { get; init; }
}
