using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class BuildFile
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("size")] public double Size { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("subType")] public string SubType { get; set; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; set; }
}
