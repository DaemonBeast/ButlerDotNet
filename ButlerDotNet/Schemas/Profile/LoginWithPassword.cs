using System.Text.Json.Serialization;

namespace ButlerDotNet.Schemas.Profile;

public class LoginWithPassword
{
    [JsonPropertyName("username")] public required string Username { get; init; }
    [JsonPropertyName("password")] public required string Password { get; init; }
    [JsonPropertyName("forceRecaptcha")] public bool? ForceRecaptcha { get; init; }

    public class Result
    {
        [JsonPropertyName("profile")] public required Structs.Profile Profile { get; init; }
        [JsonPropertyName("cookie")] public required CookieData Cookie { get; init; }

        public class CookieData
        {
            [JsonPropertyName("itchio")] public required string ItchIo { get; init; }
        }
    }
}
