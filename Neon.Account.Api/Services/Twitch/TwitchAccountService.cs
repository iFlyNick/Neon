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
        
        //generate twitch account auth object
        var dbAuth = new TwitchAccountAuth
        {
            AuthorizationCode = userAuth.AuthenticationResponse.Code,
            AccessToken = userAuth.OAuthResponse.AccessToken,
            RefreshToken = userAuth.OAuthResponse.RefreshToken,
            LastRefreshDate = curDate,
            LastValidationDate = curDate
        };
        
        //generate twitch account scope
        var grantedScopes = userAuth.OAuthValidationResponse.Scopes;
        var dbScopes = await GetTwitchAccountScopes(grantedScopes, ct);
        
        //put it all together to generate the full twitch account object for db persist
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
            TwitchAccountAuth = dbAuth,
            TwitchAccountScopes = dbScopes
        };

        await twitchDbService.UpsertTwitchAccountAsync(dbAccount, ct);
    }

    private async Task<List<TwitchAccountScope>?> GetTwitchAccountScopes(List<string>? grantedScopes, CancellationToken ct = default)
    {
        var dbAuthScopes = await twitchDbService.GetAuthorizationScopesByNameAsync(grantedScopes, ct);

        if (dbAuthScopes is null || dbAuthScopes.Count == 0)
        {
            logger.LogWarning("No auth scopes found in db for account modification!");
            return null;
        }
        
        var dbScopes = new List<TwitchAccountScope>();
        foreach (var scope in dbAuthScopes)
        {
            var tVal = new TwitchAccountScope
            {
                AuthorizationScopeId = scope.AuthorizationScopeId
            };
            
            dbScopes.Add(tVal);
        }

        if (dbScopes.Count == 0)
            return null;
        
        return dbScopes;
    }
}
