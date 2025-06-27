using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class Profile
{
    [JsonPropertyName("id")] public required long Id { get; init; }
    [JsonPropertyName("lastConnected")] public required DateTimeOffset LastConnected { get; init; }
    [JsonPropertyName("user")] public required User User { get; init; }
}
