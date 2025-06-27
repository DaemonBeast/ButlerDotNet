using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class Upload
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("storage")] public string Storage { get; set; }
    [JsonPropertyName("host")] public string Host { get; set; }
    [JsonPropertyName("filename")] public string FileName { get; set; }
    [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    [JsonPropertyName("size")] public double Size { get; set; }
    [JsonPropertyName("channelName")] public string ChannelName { get; set; }
    [JsonPropertyName("build")] public Build Build { get; set; }
    [JsonPropertyName("buildId")] public long BuildId { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("preorder")] public bool PreOrder { get; set; }
    [JsonPropertyName("demo")] public bool Demo { get; set; }
    [JsonPropertyName("platforms")] public Platforms Platforms { get; set; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; set; }
}
