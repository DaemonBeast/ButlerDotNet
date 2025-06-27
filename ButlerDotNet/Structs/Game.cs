using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class Game
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; }
    [JsonPropertyName("shortText")] public string ShortText { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("classification")] public string Classification { get; set; }
    [JsonPropertyName("embed")] public string Embed { get; set; }
    [JsonPropertyName("coverUrl")] public string CoverUrl { get; set; }
    [JsonPropertyName("stillCoverUrl")] public string StillCoverUrl { get; set; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; }
    [JsonPropertyName("publishedAt")] public string PublishedAt { get; set; }
    [JsonPropertyName("minPrice")] public long MinPrice { get; set; }
    [JsonPropertyName("canBeBought")] public bool CanBeBought { get; set; }
    [JsonPropertyName("hasDemo")] public bool HasDemo { get; set; }
    [JsonPropertyName("inPressSystem")] public bool InPressSystem { get; set; }
    [JsonPropertyName("platforms")] public Platforms Platforms { get; set; }
    [JsonPropertyName("user")] public User User { get; set; }
    [JsonPropertyName("userId")] public long UserId { get; set; }
    [JsonPropertyName("sale")] public Sale Sale { get; set; }
    [JsonPropertyName("viewsCount")] public long ViewsCount { get; set; }
    [JsonPropertyName("downloadsCount")] public long DownloadsCount { get; set; }
    [JsonPropertyName("purchasesCount")] public long PurchasesCount { get; set; }
    [JsonPropertyName("published")] public bool Published { get; set; }
}
