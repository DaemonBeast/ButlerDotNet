using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ButlerDotNet.Utilities;

namespace ButlerDotNet.Schemas.Butlerd;

public class ListenNotificationHandler(ButlerClient butlerClient) :
    BaseButlerdNotificationHandler<ListenNotificationHandler, ListenNotification>,
    IButlerdNotificationHandler
{
    public override string Type => "butlerd/listen-notification";

    public override ButlerClient ButlerClient { get; } = butlerClient;

    public static BaseButlerdNotificationHandler Create(ButlerClient butlerClient)
        => new ListenNotificationHandler(butlerClient);

    public override void Handle(ListenNotification listenNotification)
    {
        if (!listenNotification.Tcp.TryGetHostnameAndPort(out var hostname, out var port))
        {
            throw new Exception("Failed to parse hostname and port in listen notification.");
        }

        ButlerClient.InitializeSocket(hostname, port.Value, listenNotification.Secret);
    }
}

public class ListenNotification
{
    [JsonPropertyName("secret")] public required string Secret { get; init; }
    [JsonPropertyName("tcp")] public required TcpData Tcp { get; init; }
    [JsonPropertyName("time")] public required long Time { get; init; }

    public DateTimeOffset GetTime()
        => DateTimeOffset.FromUnixTimeSeconds(Time);

    public class TcpData
    {
        [JsonPropertyName("address")] public required string Address { get; init; }

        public bool TryGetHostnameAndPort(
            [NotNullWhen(true)] out string? hostname,
            [NotNullWhen(true)] out int? port)
            => AddressUtils.TryParseIPv4(Address, out hostname, out port);
    }
}
