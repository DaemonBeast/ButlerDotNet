using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class User
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; }
    [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    [JsonPropertyName("developer")] public bool Developer { get; set; }
    [JsonPropertyName("pressUsername")] public bool PressUser { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; }
    [JsonPropertyName("coverUrl")] public string CoverUrl { get; set; }
    [JsonPropertyName("stillCoverUrl")] public string StillCoverUrl { get; set; }
}
