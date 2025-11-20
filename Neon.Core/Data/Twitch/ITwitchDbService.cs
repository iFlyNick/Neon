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
    Task<List<string>?> GetAllSubscribedChannelBroadcasterIds(CancellationToken ct = default);
    Task<List<TwitchChatOverlaySettings>?> GetAllChatOverlaySettingsByBroadcasterId(string? broadcasterId,
        CancellationToken ct = default);
    Task<TwitchAccount?> GetTwitchAccountDetailForStreamElementsAuth(string? broadcasterId,
        CancellationToken ct = default);
    
    //se auth
    Task<int> UpsertStreamElementsAuthForTwitchAccount(TwitchAccount? twitchAccount, string? seChannel, string? jwtToken, CancellationToken ct = default);
    Task<StreamElementsAuth?> GetStreamElementsAuthForTwitchAccount(string? broadcasterId, CancellationToken ct = default);
    Task<List<TwitchAccount>?> GetAllAccountsWithStreamElementAuths(CancellationToken ct = default);
}
