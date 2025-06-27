using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class CaveStats
{
    [JsonPropertyName("installedAt")] public string InstalledAt { get; set; }
    [JsonPropertyName("lastTouchedAt")] public string LastTouchedAt { get; set; }
    [JsonPropertyName("secondsRun")] public double SecondsRun { get; set; }
}
