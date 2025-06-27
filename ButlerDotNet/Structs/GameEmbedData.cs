using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class GameEmbedData
{
    [JsonPropertyName("gameId")] public long GameId { get; set; }
    [JsonPropertyName("width")] public double Width { get; set; }
    [JsonPropertyName("height")] public double Height { get; set; }
    [JsonPropertyName("fullscreen")] public bool Fullscreen { get; set; }
}
