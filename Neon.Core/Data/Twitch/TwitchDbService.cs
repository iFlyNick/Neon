using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neon.Persistence.EntityModels.Twitch;
using Neon.Persistence.NeonContext;
using System.Net.Http.Headers;

namespace Neon.Core.Data.Twitch;

public class TwitchDbService(ILogger<TwitchDbService> logger, NeonDbContext context) : ITwitchDbService
{
    private readonly ILogger<TwitchDbService> _logger = logger;
    private readonly NeonDbContext _context = context;

    public async Task<TwitchAccount?> GetNeonBotTwitchAccountAsync(CancellationToken ct = default)
    {
        if (_context.TwitchAccount is null)
        {
            _logger.LogError("TwitchAccount context is null!");
            return null;
        }

        return await _context.TwitchAccount.FirstOrDefaultAsync(s => s.BroadcasterId == "801173166", ct);
    }

    public async Task<BotAccount?> GetBotAccountAsync(string? botName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(botName))
        {
            _logger.LogDebug("Invalid bot name request. BotName: {botName}", botName);
            return null;
        }

        if (_context.BotAccount is null)
        {
            _logger.LogError("BotAccount context is null!");
            return null;
        }

        var resp = await _context.BotAccount.AsNoTracking().FirstOrDefaultAsync(s => s.BotName == botName, ct);

        return resp;
    }

    public async Task<int> UpdateBotAccountSettingsAsync(BotAccount? account, CancellationToken ct = default)
    {
        if (account is null)
            return 0;

        if (_context.BotAccount is null)
        {
            _logger.LogError("BotAccount context is null!");
            return 0;
        }

        var dbAccount = await _context.BotAccount.FirstOrDefaultAsync(s => s.BotName == account.BotName, ct);

        if (dbAccount is null)
        {
            _logger.LogWarning("Bot account not found. BotName: {botName}", account.BotName);
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
            _logger.LogError("BotAccount context is null!");
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

        return await _context.TwitchAccount.AsNoTracking().Where(s => !(s.IsAuthorizationRevoked ?? true) && s.BroadcasterId != "801173166").ToListAsync(ct);
    }

    private async Task<TwitchAccount?> GetTwitchAccountAsync(string? broadcasterId, CancellationToken ct = default)
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

    public async Task<int> UpdateTwitchAccountAuthAsync(string? broadcasterId, string? accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger.LogDebug("Invalid broadcaster id request. BroadcasterId: {broadcasterId}", broadcasterId);
            return 0;
        }

        var account = await GetTwitchAccountAsync(broadcasterId, ct);

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
