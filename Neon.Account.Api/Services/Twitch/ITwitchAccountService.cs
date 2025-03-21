using Neon.Account.Api.Models;

namespace Neon.Account.Api.Services.Twitch;

public interface ITwitchAccountService
{
    Task CreateTwitchAccountFromOAuthAsync(TwitchUserAccountAuth? userAuth, CancellationToken ct = default);
}
