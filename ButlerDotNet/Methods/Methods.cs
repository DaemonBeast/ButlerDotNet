namespace ButlerDotNet.Methods;

public partial class Methods
{
    public FetchMethods Fetch { get; }

    public ProfileMethods Profile { get; }

    internal Methods(Session session)
    {
        Fetch = new FetchMethods(session);
        Profile = new ProfileMethods(session);
    }
}
