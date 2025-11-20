using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neon.Core.Services.Encryption;
using Neon.Persistence.EntityModels.Twitch;
using Neon.Persistence.NeonContext;

namespace Neon.Core.Data.Twitch;

public class TwitchDbService(ILogger<TwitchDbService> logger, NeonDbContext context, IEncryptionService encryptionService) : ITwitchDbService
{
    public async Task<AppAccount?> GetAppAccountAsync(string? appName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(appName))
        {
            logger.LogDebug("Invalid bot name request. BotName: {botName}", appName);
            return null;
        }

        var resp = await context.AppAccount.AsNoTracking().FirstOrDefaultAsync(s => s.AppName == appName, ct);

        if (resp is null)
            return null;
        
        resp.ClientSecret = encryptionService.Decrypt(resp.ClientSecret, resp.ClientSecretIv);
        resp.AccessToken = encryptionService.Decrypt(resp.AccessToken, resp.AccessTokenIv);
        
        return resp;
    }

    public async Task<List<SubscriptionType>?> GetDefaultSubscriptionsAsync(CancellationToken ct = default)
    {
        var defaultSubscriptionTypes = new List<string> { "channel.update", "stream.offline", "stream.online" };
        
        var resp = await context.SubscriptionType.AsNoTracking().Where(s => !string.IsNullOrEmpty(s.Name) && defaultSubscriptionTypes.Contains(s.Name)).ToListAsync(ct);

        return resp;
    }

    public async Task<List<AuthorizationScope>?> GetAuthorizationScopesByNameAsync(List<string>? names, CancellationToken ct = default)
    {
        if (names is null || names.Count == 0)
            return null;
        
        var resp = await context.AuthorizationScope.AsNoTracking().Where(s => !string.IsNullOrEmpty(s.Name) && names.Contains(s.Name)).ToListAsync(ct);

        return resp;
    }
    
    public async Task<TwitchAccount?> GetTwitchAccountByBroadcasterName(string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterName))
        {
            logger.LogDebug("Invalid broadcaster name request. BroadcasterName: {broadcasterName}", broadcasterName);
            return null;
        }

        var resp = await context.TwitchAccount
            .Include(s => s.TwitchAccountAuth!)
            .Include(s => s.TwitchAccountScopes!)
                .ThenInclude(s => s.AuthorizationScope)
                    .ThenInclude(s => s.AuthorizationScopeSubscriptionTypes)
                        .ThenInclude(s => s.SubscriptionType)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => !string.IsNullOrEmpty(s.LoginName) && s.LoginName.ToLower() == broadcasterName.ToLower(), ct);

        if (resp is null)
            return null;

        if (resp.TwitchAccountAuth is not null)
        {
            resp.TwitchAccountAuth.AccessToken = encryptionService.Decrypt(resp.TwitchAccountAuth.AccessToken, resp.TwitchAccountAuth.AccessTokenIv);
            resp.TwitchAccountAuth.RefreshToken = encryptionService.Decrypt(resp.TwitchAccountAuth.RefreshToken, resp.TwitchAccountAuth.RefreshTokenIv);
        }
        
        return resp;
    }
    
    public async Task<TwitchAccount?> GetTwitchAccountByBroadcasterIdAsync(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogDebug("Invalid broadcaster id request. BroadcasterId: {broadcasterId}", broadcasterId);
            return null;
        }

        var resp = await context.TwitchAccount
            .Include(s => s.TwitchAccountAuth!)
            .Include(s => s.TwitchAccountScopes!)
                .ThenInclude(s => s.AuthorizationScope)
                    .ThenInclude(s => s.AuthorizationScopeSubscriptionTypes)
                        .ThenInclude(s => s.SubscriptionType)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BroadcasterId == broadcasterId, ct);

        if (resp is null)
            return null;

        if (resp.TwitchAccountAuth is not null)
        {
            resp.TwitchAccountAuth.AccessToken = encryptionService.Decrypt(resp.TwitchAccountAuth.AccessToken, resp.TwitchAccountAuth.AccessTokenIv);
            resp.TwitchAccountAuth.RefreshToken = encryptionService.Decrypt(resp.TwitchAccountAuth.RefreshToken, resp.TwitchAccountAuth.RefreshTokenIv);
        }
        
        return resp;
    }
    
    public async Task<int> UpdateAppAccountSettingsAsync(AppAccount? account, CancellationToken ct = default)
    {
        if (account is null)
            return 0;

        var dbAccount = await context.AppAccount.FirstOrDefaultAsync(s => s.AppName == account.AppName, ct);

        if (dbAccount is null)
        {
            logger.LogWarning("App account not found. AppName: {AppName}", account.AppName);
            return 0;
        }

        var clientSecretEncryption = encryptionService.Encrypt(account.ClientSecret);
        dbAccount.ClientSecret = clientSecretEncryption.Item1;
        dbAccount.ClientSecretIv = clientSecretEncryption.Item2;
        
        var accessTokenEncryption = encryptionService.Encrypt(account.AccessToken);
        dbAccount.AccessToken = accessTokenEncryption.Item1;
        dbAccount.AccessTokenIv = accessTokenEncryption.Item2;

        return await context.SaveChangesAsync(ct);
    }

    public async Task<int> UpsertTwitchAccountAsync(TwitchAccount? account, CancellationToken ct = default)
    {
        if (account is null)
            return 0;

        if (account.TwitchAccountAuth is not null)
        {
            var accessTokenEncryption = encryptionService.Encrypt(account.TwitchAccountAuth.AccessToken);
            account.TwitchAccountAuth.AccessToken = accessTokenEncryption.Item1;
            account.TwitchAccountAuth.AccessTokenIv = accessTokenEncryption.Item2;
            
            var refreshTokenEncryption = encryptionService.Encrypt(account.TwitchAccountAuth.RefreshToken);
            account.TwitchAccountAuth.RefreshToken = refreshTokenEncryption.Item1;
            account.TwitchAccountAuth.RefreshTokenIv = refreshTokenEncryption.Item2;
        }
        
        var dbAccount = 
            await context.TwitchAccount
                .Include(s => s.TwitchAccountAuth)
                .Include(s => s.TwitchAccountScopes)
                .FirstOrDefaultAsync(s => s.BroadcasterId == account.BroadcasterId, ct);

        if (dbAccount is null)
        {
            logger.LogDebug("Creating local twitch account for broadcaster: {broadcasterId}", account.BroadcasterId);
            
            context.TwitchAccount.Add(account);

            return await context.SaveChangesAsync(ct);
        }

        //update db account with new details. could be that they revoked and re-added, or simply changed details of their account and this was recalled by the app
        dbAccount.LoginName = account.LoginName;
        dbAccount.DisplayName = account.DisplayName;
        dbAccount.Type = account.Type;
        dbAccount.BroadcasterType = account.BroadcasterType;
        dbAccount.ProfileImageUrl = account.ProfileImageUrl;
        dbAccount.OfflineImageUrl = account.OfflineImageUrl;
        dbAccount.AccountCreatedDate = account.AccountCreatedDate;
        dbAccount.NeonAuthorizationDate = account.NeonAuthorizationDate;
        dbAccount.NeonAuthorizationRevokeDate = account.NeonAuthorizationRevokeDate;
        dbAccount.IsAuthorizationRevoked = account.IsAuthorizationRevoked;
        
        //update auth details
        if (account.TwitchAccountAuth is not null)
        {
            if (dbAccount.TwitchAccountAuth is null)
            {
                dbAccount.TwitchAccountAuth = account.TwitchAccountAuth;
            }
            else
            {
                dbAccount.TwitchAccountAuth.AccessToken = account.TwitchAccountAuth.AccessToken;
                dbAccount.TwitchAccountAuth.AccessTokenIv = account.TwitchAccountAuth.AccessTokenIv;
                dbAccount.TwitchAccountAuth.RefreshToken = account.TwitchAccountAuth.RefreshToken;
                dbAccount.TwitchAccountAuth.RefreshTokenIv = account.TwitchAccountAuth.RefreshTokenIv;
                dbAccount.TwitchAccountAuth.LastRefreshDate = account.TwitchAccountAuth.LastRefreshDate;
                dbAccount.TwitchAccountAuth.LastValidationDate = account.TwitchAccountAuth.LastValidationDate;
            }
        }
        
        //update scopes
        if (account.TwitchAccountScopes is not null && account.TwitchAccountScopes.Count > 0)
        {
            var expectedScopes = account.TwitchAccountScopes.Select(s => s.AuthorizationScopeId).ToList();
            var removeScopes = dbAccount.TwitchAccountScopes?.Where(s => !expectedScopes.Contains(s.AuthorizationScopeId)).ToList();
            if (removeScopes is not null && removeScopes.Count > 0)
                context.TwitchAccountScope.RemoveRange(removeScopes);
            
            foreach (var scope in account.TwitchAccountScopes)
            {
                var dbScope = dbAccount.TwitchAccountScopes?.FirstOrDefault(s => s.AuthorizationScopeId == scope.AuthorizationScopeId);
                if (dbScope is null)
                    dbAccount.TwitchAccountScopes?.Add(scope);
            }
        }
        
        context.TwitchAccount.Update(dbAccount);

        if (context.Entry(dbAccount).State == EntityState.Unchanged)
            return 0;

        if (context.ChangeTracker.HasChanges())
        {
            logger.LogDebug("Updating local twitch account for broadcaster: {broadcasterId}", account.BroadcasterId);
            return await context.SaveChangesAsync(ct);
        }

        return 0;
    }

    public async Task<int> UpdateTwitchAccountAuthAsync(string? broadcasterId, string? accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogDebug("Invalid broadcaster id request. BroadcasterId: {broadcasterId}", broadcasterId);
            return 0;
        }

        var account = await context.TwitchAccount.Include(s => s.TwitchAccountAuth).FirstOrDefaultAsync(s => s.BroadcasterId == broadcasterId, ct);

        if (account is null)
        {
            logger.LogWarning("Twitch account not found. BroadcasterId: {broadcasterId}", broadcasterId);
            return 0;
        }
        
        if (account.TwitchAccountAuth is null)
        {
            logger.LogWarning("Twitch account auth not found. BroadcasterId: {broadcasterId}", broadcasterId);
            return 0;
        }
        
        var accessTokenEncryption = encryptionService.Encrypt(accessToken);
        account.TwitchAccountAuth.AccessToken = accessTokenEncryption.Item1;
        account.TwitchAccountAuth.AccessTokenIv = accessTokenEncryption.Item2;
        
        account.TwitchAccountAuth.LastRefreshDate = DateTime.UtcNow;

        return await context.SaveChangesAsync(ct);
    }

    public async Task<List<TwitchAccount>?> GetAllSubscribedChannelAccounts(CancellationToken ct = default)
    {
        var accounts = await context.TwitchAccount.AsNoTracking().Where(s => !string.IsNullOrEmpty(s.LoginName) && s.LoginName.ToLower() != "theneonbot" && !(s.IsAuthorizationRevoked ?? false)).ToListAsync(ct);
        
        if (accounts.Count == 0)
        {
            logger.LogWarning("No subscribed twitch accounts found.");
            return null;
        }

        return accounts;
    }

    public async Task<List<string>?> GetAllSubscribedChannelBroadcasterIds(CancellationToken ct = default)
    {
        var accountIds = await context.TwitchAccount.AsNoTracking().Where(s => !string.IsNullOrEmpty(s.LoginName) && !(s.IsAuthorizationRevoked ?? false)).Select(s => s.BroadcasterId!).ToListAsync(ct);

        if (accountIds.Count != 0) 
            return accountIds;
        
        logger.LogWarning("No subscribed twitch accounts found.");
        return null;

    }

    public async Task<List<TwitchChatOverlaySettings>?> GetAllChatOverlaySettingsByBroadcasterId(string? broadcasterId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogDebug("No broadcaster id passed to fetch chat overlay settings method.");
            return null;
        }

        var overlaySettings = await context.TwitchAccount.Include(s => s.TwitchChatOverlaySettings)
            .FirstOrDefaultAsync(s => s.BroadcasterId == broadcasterId, ct);
        
        return overlaySettings?.TwitchChatOverlaySettings?.ToList();
    }

    public async Task<TwitchAccount?> GetTwitchAccountDetailForStreamElementsAuth(string? broadcasterId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
            return null;
        
        var resp = await context.TwitchAccount.AsNoTracking().FirstOrDefaultAsync(s => s.BroadcasterId == broadcasterId, ct);
        
        return resp;
    }
    
    public async Task<int> UpsertStreamElementsAuthForTwitchAccount(TwitchAccount? twitchAccount, string? seChannel, string? jwtToken, CancellationToken ct = default)
    {
        if (twitchAccount is null || string.IsNullOrEmpty(seChannel) || string.IsNullOrEmpty(jwtToken))
            return 0;

        var dbAccount = await context.TwitchAccount.Include(s => s.StreamElementsAuth).FirstOrDefaultAsync(s => s.TwitchAccountId == twitchAccount.TwitchAccountId, ct);

        if (dbAccount is null)
        {
            logger.LogError("Twitch account not found when trying to upsert StreamElements auth. BroadcasterId: {broadcasterId}", twitchAccount.BroadcasterId);
            return 0;
        }

        if (dbAccount.StreamElementsAuth is null)
        {
            logger.LogDebug("Creating new StreamElements auth for Twitch account. BroadcasterId: {broadcasterId}", twitchAccount.BroadcasterId);

            var (seAuthToken, seAuthIv) = encryptionService.Encrypt(jwtToken);

            var seAuth = new StreamElementsAuth
            {
                TwitchAccountId = twitchAccount.TwitchAccountId,
                StreamElementsChannel = seChannel,
                JwtToken = seAuthToken,
                JwtTokenIv = seAuthIv
            };
            
            context.StreamElementsAuth.Add(seAuth);
        }
        else
        {
            logger.LogDebug("Updating StreamElements auth for Twitch account. BroadcasterId: {broadcasterId}", twitchAccount.BroadcasterId);
            
            var seAuthEncrypted = encryptionService.Encrypt(jwtToken);
            dbAccount.StreamElementsAuth.StreamElementsChannel = seChannel;
            dbAccount.StreamElementsAuth.JwtToken = seAuthEncrypted.Item1;
            dbAccount.StreamElementsAuth.JwtTokenIv = seAuthEncrypted.Item2;
        }

        if (context.ChangeTracker.HasChanges())
            return await context.SaveChangesAsync(ct);

        return 0;
    }
    
    public async Task<StreamElementsAuth?> GetStreamElementsAuthForTwitchAccount(string? broadcasterId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
            return null;

        var dbAccount = await context.TwitchAccount.Include(s => s.StreamElementsAuth).AsNoTracking()
            .FirstOrDefaultAsync(s => s.BroadcasterId == broadcasterId, ct);
        
        if (dbAccount?.StreamElementsAuth is null)
            return null;

        dbAccount.StreamElementsAuth.JwtToken = encryptionService.Decrypt(dbAccount.StreamElementsAuth.JwtToken,
            dbAccount.StreamElementsAuth.JwtTokenIv);
        
        return dbAccount.StreamElementsAuth;
    }

    public async Task<List<TwitchAccount>?> GetAllAccountsWithStreamElementAuths(CancellationToken ct = default)
    {
        var accounts = await context.TwitchAccount.Include(s => s.StreamElementsAuth).AsNoTracking().Where(s => s.StreamElementsAuth != null).ToListAsync(ct);

        var retList = new List<TwitchAccount>();
        
        foreach (var account in accounts)
        {
            if (account?.StreamElementsAuth is null)
                continue;
            
            account.StreamElementsAuth.JwtToken = encryptionService.Decrypt(account.StreamElementsAuth.JwtToken, account.StreamElementsAuth.JwtTokenIv);
            
            retList.Add(account);
        }
        
        return retList;
    }
}
