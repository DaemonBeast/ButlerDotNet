using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class Build
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("parentBuildId")] public long ParentBuildId { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
    [JsonPropertyName("version")] public long Version { get; set; }
    [JsonPropertyName("userVersion")] public string UserVersion { get; set; }
    [JsonPropertyName("files")] public BuildFile[] Files { get; set; }
    [JsonPropertyName("user")] public User User { get; set; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; set; }
}
