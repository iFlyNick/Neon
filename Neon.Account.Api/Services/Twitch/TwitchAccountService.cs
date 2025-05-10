using Neon.Account.Api.Models;
using Neon.Core.Data.Twitch;
using Neon.Persistence.EntityModels.Twitch;

namespace Neon.Account.Api.Services.Twitch;

public class TwitchAccountService(ILogger<TwitchAccountService> logger, ITwitchDbService twitchDbService) : ITwitchAccountService
{
    public async Task CreateTwitchAccountFromOAuthAsync(TwitchUserAccountAuth? userAuth, CancellationToken ct = default)
    {
        //need to build a local account from the entire oauth process. at this point we should have most of the details we need to ensure user tokens are saved to the db for any access later
        if (userAuth is null || userAuth.AuthenticationResponse is null || userAuth.OAuthResponse is null || userAuth.OAuthValidationResponse is null || userAuth.TwitchUserAccount is null)
        {
            logger.LogError("Invalid user auth object received. Unable to create local twitch account representation!");
            return;
        }

        var curDate = DateTime.UtcNow;

        var dbAccount = new TwitchAccount
        {
            BroadcasterId = userAuth.TwitchUserAccount.BroadcasterId,
            LoginName = userAuth.TwitchUserAccount.LoginName,
            DisplayName = userAuth.TwitchUserAccount.DisplayName,
            Type = userAuth.TwitchUserAccount.Type,
            BroadcasterType = userAuth.TwitchUserAccount.BroadcasterType,
            ProfileImageUrl = userAuth.TwitchUserAccount.ProfileImageUrl,
            OfflineImageUrl = userAuth.TwitchUserAccount.OfflineImageUrl,
            AccountCreatedDate = userAuth.TwitchUserAccount.CreatedAt,
            NeonAuthorizationDate = curDate,
            IsAuthorizationRevoked = false,
            AuthorizationCode = userAuth.AuthenticationResponse.Code,
            AccessToken = userAuth.OAuthResponse.AccessToken,
            RefreshToken = userAuth.OAuthResponse.RefreshToken,
            AccessTokenRefreshDate = curDate,
            AuthorizationScopes = (userAuth.OAuthValidationResponse.Scopes is null || userAuth.OAuthValidationResponse.Scopes.Count == 0) ? "" : string.Join(",", userAuth.OAuthValidationResponse.Scopes)
        };

        await twitchDbService.UpsertTwitchAccountAsync(dbAccount, ct);
    }
}
