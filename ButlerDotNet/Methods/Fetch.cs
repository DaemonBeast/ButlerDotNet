using ButlerDotNet.Schemas.Fetch;

namespace ButlerDotNet.Methods;

public class FetchMethods
{
    private readonly Session _session;

    internal FetchMethods(Session session)
    {
        _session = session;
    }

    public Task<GameUploads.Result> GameUploads(long gameId, bool compatible, bool? fresh = null)
        => _session.SendRequest<GameUploads.Result>(
            "Fetch.GameUploads",
            new GameUploads { GameId = gameId, Compatible = compatible, Fresh = fresh });
}
