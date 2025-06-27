using System.Text.Json.Serialization;

namespace ButlerDotNet.Structs;

public class Sale
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("gameId")] public long GameId { get; set; }
    [JsonPropertyName("rate")] public double Rate { get; set; }
    [JsonPropertyName("startDate")] public string StartDate { get; set; }
    [JsonPropertyName("endDate")] public string EndDate { get; set; }
}
