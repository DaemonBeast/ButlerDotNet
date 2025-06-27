using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class UpgradeInstallEvent
{
    [JsonPropertyName("numPatches")] public required long NumPatches { get; init; }
}
