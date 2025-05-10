using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neon.Core.Models.Twitch.EventSub;
using Neon.Persistence.EntityModels.Twitch;
using Neon.Persistence.NeonContext;

namespace Neon.Core.Data.Twitch;

public class TwitchDbService(ILogger<TwitchDbService> logger, NeonDbContext context) : ITwitchDbService
{
    public async Task<AppAccount?> GetAppAccountAsync(string? appName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(appName))
        {
            logger.LogDebug("Invalid bot name request. BotName: {botName}", appName);
            return null;
        }

        var resp = await context.AppAccount.AsNoTracking().FirstOrDefaultAsync(s => s.AppName == appName, ct);

        return resp;
    }

    public async Task<List<SubscriptionType>?> GetSubscriptionsAsync(CancellationToken ct = default)
    {
        var defaultSubscriptionTypes = new List<string> { "channel.update", "stream.offline", "stream.online" };
        
        var resp = await context.SubscriptionType.AsNoTracking().Where(s => !string.IsNullOrEmpty(s.Name) && defaultSubscriptionTypes.Contains(s.Name)).ToListAsync(ct);

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
            .Include(s => s.TwitchAccountAuth)
            .Include(s => s.TwitchAccountScopes)
                .ThenInclude(s => s.AuthorizationScope)
                    .ThenInclude(s => s.AuthorizationScopeSubscriptionTypes)
                        .ThenInclude(s => s.SubscriptionType)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => !string.IsNullOrEmpty(s.LoginName) && s.LoginName.ToLower() == broadcasterName.ToLower(), ct);

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
            .Include(s => s.TwitchAccountAuth)
            .Include(s => s.TwitchAccountScopes)
                .ThenInclude(s => s.AuthorizationScope)
                    .ThenInclude(s => s.AuthorizationScopeSubscriptionTypes)
                        .ThenInclude(s => s.SubscriptionType)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BroadcasterId == broadcasterId, ct);

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

        dbAccount.AccessToken = account.AccessToken;

        return await context.SaveChangesAsync(ct);
    }

    public async Task<int> UpsertTwitchAccountAsync(TwitchAccount? account, CancellationToken ct = default)
    {
        if (account is null)
            return 0;

        var dbAccount = await context.TwitchAccount.FirstOrDefaultAsync(s => s.BroadcasterId == account.BroadcasterId, ct);

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
        dbAccount.IsAuthorizationRevoked = account.IsAuthorizationRevoked;

        if (context.ChangeTracker.HasChanges())
            return await context.SaveChangesAsync(ct);

        return 0;
    }

    public async Task<List<TwitchAccount>?> GetSubscribedTwitchAccountsAsync(CancellationToken ct = default)
    {
        //TODO: remove hardcoded login name and make direct field for subscribed account for chat joining?
        return await context.TwitchAccount.AsNoTracking().Where(s => !(s.IsAuthorizationRevoked ?? true) && s.LoginName != "theneonbot").ToListAsync(ct);
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

        account.TwitchAccountAuth.AccessToken = accessToken;
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
}
