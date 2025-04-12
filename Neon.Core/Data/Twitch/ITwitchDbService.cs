using Neon.Persistence.EntityModels.Twitch;

namespace Neon.Core.Data.Twitch;

public interface ITwitchDbService
{
    Task<BotAccount?> GetBotAccountAsync(string? botName, CancellationToken ct = default);
    Task<int> UpdateBotAccountSettingsAsync(BotAccount? account, CancellationToken ct = default);
    Task<int> UpsertTwitchAccountAsync(TwitchAccount? account, CancellationToken ct = default);
    Task<List<TwitchAccount>?> GetSubscribedTwitchAccountsAsync(CancellationToken ct = default);
    Task<TwitchAccount?> GetTwitchAccountByBroadcasterName(string? broadcasterName, CancellationToken ct = default);
    Task<int> UpdateTwitchAccountAuthAsync(string? broadcasterId, string? accessToken, CancellationToken ct = default);
    Task<TwitchAccount?> GetNeonBotTwitchAccountAsync(CancellationToken ct = default);
}
