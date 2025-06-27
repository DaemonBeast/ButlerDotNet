using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class CaveInstallInfo
{
    [JsonPropertyName("installedSize")] public double InstalledSize { get; set; }
    [JsonPropertyName("installLocation")] public string InstallLocation { get; set; }
    [JsonPropertyName("installFolder")] public string InstallFolder { get; set; }
    [JsonPropertyName("pinned")] public bool Pinned { get; set; }
}
