using Neon.Persistence.EntityModels.Twitch;

namespace Neon.Core.Data.Twitch;

public interface ITwitchDbService
{
    Task<AppAccount?> GetAppAccountAsync(string? appName, CancellationToken ct = default);
    Task<List<SubscriptionType>?> GetDefaultSubscriptionsAsync(CancellationToken ct = default);
    Task<List<AuthorizationScope>?> GetAuthorizationScopesByNameAsync(List<string>? names, CancellationToken ct = default);
    Task<TwitchAccount?> GetTwitchAccountByBroadcasterName(string? broadcasterName, CancellationToken ct = default);
    Task<TwitchAccount?> GetTwitchAccountByBroadcasterIdAsync(string? broadcasterId, CancellationToken ct = default);
    Task<int> UpdateAppAccountSettingsAsync(AppAccount? account, CancellationToken ct = default);
    Task<int> UpsertTwitchAccountAsync(TwitchAccount? account, CancellationToken ct = default);
    Task<int> UpdateTwitchAccountAuthAsync(string? broadcasterId, string? accessToken, CancellationToken ct = default);
    Task<List<TwitchAccount>?> GetAllSubscribedChannelAccounts(CancellationToken ct = default);
    Task<List<TwitchChatOverlaySettings>?> GetAllChatOverlaySettingsByBroadcasterId(string? broadcasterId,
        CancellationToken ct = default);
}
