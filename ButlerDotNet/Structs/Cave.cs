using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class Cave
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("game")] public Game Game { get; set; }
    [JsonPropertyName("upload")] public Upload Upload { get; set; }
    [JsonPropertyName("build")] public Build Build { get; set; }
    [JsonPropertyName("stats")] public CaveStats CaveStats { get; set; }
    [JsonPropertyName("installInfo")] public CaveInstallInfo InstallInfo { get; set; }
}
