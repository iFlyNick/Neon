using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neon.Persistence.EntityModels.Twitch;
using Neon.Persistence.NeonContext;

namespace Neon.Core.Data.Twitch;

public class TwitchDbService(ILogger<TwitchDbService> logger, NeonDbContext context) : ITwitchDbService
{
    private readonly ILogger<TwitchDbService> _logger = logger;
    private readonly NeonDbContext _context = context;

    public async Task<AppAccount?> GetAppAccountAsync(string? appName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(appName))
        {
            _logger.LogDebug("Invalid bot name request. BotName: {botName}", appName);
            return null;
        }

        if (_context.AppAccount is null)
        {
            _logger.LogError("AppAccount context is null!");
            return null;
        }

        var resp = await _context.AppAccount.AsNoTracking().FirstOrDefaultAsync(s => s.AppName == appName, ct);

        return resp;
    }

    public async Task<TwitchAccount?> GetTwitchAccountByBroadcasterName(string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterName))
        {
            _logger.LogDebug("Invalid broadcaster name request. BroadcasterName: {broadcasterName}", broadcasterName);
            return null;
        }

        return await _context.TwitchAccount!.AsNoTracking().FirstOrDefaultAsync(s => !string.IsNullOrEmpty(s.LoginName) && s.LoginName.ToLower() == broadcasterName.ToLower(), ct);
    }
    
    public async Task<TwitchAccount?> GetTwitchAccountByBroadcasterIdAsync(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger.LogDebug("Invalid broadcaster id request. BroadcasterId: {broadcasterId}", broadcasterId);
            return null;
        }

        if (_context.TwitchAccount is null)
        {
            _logger.LogError("TwitchAccount context is null!");
            return null;
        }

        return await _context.TwitchAccount.FirstOrDefaultAsync(s => s.BroadcasterId == broadcasterId, ct);
    }
    
    public async Task<int> UpdateAppAccountSettingsAsync(AppAccount? account, CancellationToken ct = default)
    {
        if (account is null)
            return 0;

        if (_context.AppAccount is null)
        {
            _logger.LogError("AppAccount context is null!");
            return 0;
        }

        var dbAccount = await _context.AppAccount.FirstOrDefaultAsync(s => s.AppName == account.AppName, ct);

        if (dbAccount is null)
        {
            _logger.LogWarning("App account not found. AppName: {AppName}", account.AppName);
            return 0;
        }

        dbAccount.AccessToken = account.AccessToken;

        return await _context.SaveChangesAsync(ct);
    }

    public async Task<int> UpsertTwitchAccountAsync(TwitchAccount? account, CancellationToken ct = default)
    {
        if (account is null)
            return 0;

        if (_context.TwitchAccount is null)
        {
            _logger.LogError("TwitchAccount context is null!");
            return 0;
        }

        var dbAccount = await _context.TwitchAccount.FirstOrDefaultAsync(s => s.BroadcasterId == account.BroadcasterId, ct);

        if (dbAccount is null)
        {
            _logger.LogDebug("Creating local twitch account for broadcaster: {broadcasterId}", account.BroadcasterId);

            _context.TwitchAccount.Add(account);

            return await _context.SaveChangesAsync(ct);
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
        dbAccount.AuthorizationCode = account.AuthorizationCode;
        dbAccount.AccessToken = account.AccessToken;
        dbAccount.RefreshToken = account.RefreshToken;
        dbAccount.AccessTokenRefreshDate = account.AccessTokenRefreshDate;
        dbAccount.AuthorizationScopes = account.AuthorizationScopes;

        if (_context.ChangeTracker.HasChanges())
            return await _context.SaveChangesAsync(ct);

        return 0;
    }

    public async Task<List<TwitchAccount>?> GetSubscribedTwitchAccountsAsync(CancellationToken ct = default)
    {
        if (_context.TwitchAccount is null)
        {
            _logger.LogError("TwitchAccount context is null!");
            return null;
        }

        //TODO: remove hardcoded login name and make direct field for subscribed account for chat joining?
        return await _context.TwitchAccount.AsNoTracking().Where(s => !(s.IsAuthorizationRevoked ?? true) && s.LoginName != "theneonbot").ToListAsync(ct);
    }

    public async Task<int> UpdateTwitchAccountAuthAsync(string? broadcasterId, string? accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger.LogDebug("Invalid broadcaster id request. BroadcasterId: {broadcasterId}", broadcasterId);
            return 0;
        }
        
        if (_context.TwitchAccount is null)
        {
            _logger.LogError("TwitchAccount context is null!");
            return 0;
        }

        var account = await _context.TwitchAccount.FirstOrDefaultAsync(s => s.BroadcasterId == broadcasterId, ct);

        if (account is null)
        {
            _logger.LogWarning("Twitch account not found. BroadcasterId: {broadcasterId}", broadcasterId);
            return 0;
        }

        account.AccessToken = accessToken;
        account.AccessTokenRefreshDate = DateTime.UtcNow;

        return await _context.SaveChangesAsync(ct);
    }
}
