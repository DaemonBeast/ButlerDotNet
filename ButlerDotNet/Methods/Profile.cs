using ButlerDotNet.Schemas.Profile;

namespace ButlerDotNet.Methods;

public partial class Methods
{
    public class ProfileMethods
    {
        private readonly Session _session;

        internal ProfileMethods(Session session)
        {
            _session = session;
        }

        public Task<LoginWithPassword.Result> LoginWithPassword(
            string username,
            string password,
            bool? forceRecaptcha = null,
            CancellationToken cancellationToken = default)
            => _session.SendRequest<LoginWithPassword.Result>(
                "Profile.LoginWithPassword",
                new LoginWithPassword { Username = username, Password = password, ForceRecaptcha = forceRecaptcha },
                cancellationToken);
    }
}
