using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class InstallEvent
{
    [JsonPropertyName("type")] public required string Type { get; init; }
    [JsonPropertyName("timestamp")] public required string Timestamp { get; init; }
    [JsonPropertyName("heal")] public HealInstallEvent? Heal { get; init; }
    [JsonPropertyName("install")] public InstallInstallEvent? Install { get; init; }
    [JsonPropertyName("upgrade")] public UpgradeInstallEvent? Upgrade { get; init; }
    [JsonPropertyName("ghostBusting")] public GhostBustingInstallEvent? GhostBusting { get; init; }
    [JsonPropertyName("patching")] public PatchingInstallEvent? Patching { get; init; }
    [JsonPropertyName("problem")] public ProblemInstallEvent? Problem { get; init; }
    [JsonPropertyName("fallback")] public FallbackInstallEvent? Fallback { get; init; }
}
