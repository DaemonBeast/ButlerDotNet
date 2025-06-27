using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class InstallInstallEvent
{
    [JsonPropertyName("manager")] public required string Manager { get; init; }
}
