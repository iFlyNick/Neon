using Neon.WebApp.Identity.Models.Twitch;

namespace Neon.WebApp.Identity.Twitch;

public interface ITwitchAccountService
{
    Task CreateTwitchAccountFromOAuthAsync(TwitchUserAccountAuth? userAuth, CancellationToken ct = default);
}