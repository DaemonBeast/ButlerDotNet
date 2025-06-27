using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class Platforms
{
    [JsonPropertyName("windows")] public string Windows { get; set; }
    [JsonPropertyName("linux")] public string Linux { get; set; }
    [JsonPropertyName("osx")] public string Osx { get; set; }
}
