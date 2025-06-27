using System.Text.Json.Serialization;
using ButlerDotNet.Structs;

namespace ButlerDotNet.Schemas.Install;

public class Queue
{
    [JsonPropertyName("caveId")] public string? CaveId { get; init; }
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("installLocationId")] public string? InstallLocationId { get; init; }
    [JsonPropertyName("noCave")] public bool? NoCave { get; init; }
    [JsonPropertyName("installFolder")] public string? InstallFolder { get; init; }
    [JsonPropertyName("game")] public Game? Game { get; init; }
    [JsonPropertyName("upload")] public Upload? Upload { get; init; }
    [JsonPropertyName("build")] public Build? Build { get; init; }
    [JsonPropertyName("ignoreInstallers")] public bool? IgnoreInstallers { get; init; }
    [JsonPropertyName("stagingFolder")] public string? StagingFolder { get; init; }
    [JsonPropertyName("queueDownload")] public bool? QueueDownload { get; init; }
    [JsonPropertyName("fastQueue")] public bool? FastQueue { get; init; }

    public class Result
    {
        [JsonPropertyName("id")] public required string Id { get; init; }
        [JsonPropertyName("reason")] public required string Reason { get; init; }
        [JsonPropertyName("caveId")] public required string CaveId { get; init; }
        [JsonPropertyName("game")] public required Game Game { get; init; }
        [JsonPropertyName("upload")] public required Upload Upload { get; init; }
        [JsonPropertyName("build")] public required Build Build { get; init; }
        [JsonPropertyName("installFolder")] public required string InstallFolder { get; init; }
        [JsonPropertyName("stagingFolder")] public required string StagingFolder { get; init; }
        [JsonPropertyName("installLocationId")] public required string InstallLocationId { get; init; }
    }
}
