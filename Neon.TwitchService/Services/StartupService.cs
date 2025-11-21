using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Services.Http;
using Neon.TwitchService.Models;
using Neon.TwitchService.Services.WebSocketManagers;

namespace Neon.TwitchService.Services;

public class StartupService(ILogger<StartupService> logger, IWebSocketManager webSocketManager, IServiceScopeFactory serviceScopeFactory, IOptions<NeonSettings> neonSettings, IOptions<NeonStartupSettings> startupSettings) : IHostedService
{
    private readonly NeonSettings _neonSettings = neonSettings.Value;
    private readonly NeonStartupSettings _startupSettings = startupSettings.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting twitch service...");
        await SubscribeAllActiveChannels();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Shutting down twitch service...");
        return Task.CompletedTask;
    }
    
    private async Task SubscribeAllActiveChannels()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var scope = serviceScopeFactory.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();
            var httpService = scope.ServiceProvider.GetRequiredService<IHttpService>();
            
            if (string.IsNullOrEmpty(_neonSettings.AppName))
            {
                logger.LogCritical("AppName is null or empty. Cannot subscribe to channels.");
                return;
            }
            
            var subscribedAccounts = await dbService.GetAllSubscribedChannelAccounts(cts.Token);
            if (subscribedAccounts is null || subscribedAccounts.Count == 0)
            {
                logger.LogInformation("No subscribed accounts found.");
                return;
            }
            
            var botChatAccount = await dbService.GetTwitchAccountByBroadcasterName(_neonSettings.AppName, cts.Token);
            if (botChatAccount is null)
            {
                logger.LogCritical("Bot chat account not found for app name: {appName}", _neonSettings.AppName);
                return;
            }

            logger.LogDebug("Sending request to emote api to preload global emotes");
            await httpService.PostAsync($"{_startupSettings.EmoteApiUrl}{_startupSettings.EmoteGlobalUri}", null, null, null, null, cts.Token);
            logger.LogDebug("Emote api global emotes preload request sent successfully");
            
            foreach (var account in subscribedAccounts)
            {
                if (string.IsNullOrEmpty(account.BroadcasterId))
                {
                    logger.LogWarning("Broadcaster id is null or empty for account: {account}", account);
                    continue;
                }
                
                await webSocketManager.Subscribe(account.BroadcasterId, cts.Token);
                logger.LogInformation("Subscribed to channel: {channelName}", account.BroadcasterId);
                
                //now connect the bot to the channel too
                await webSocketManager.SubscribeUserToChat(botChatAccount.BroadcasterId, account.BroadcasterId, cts.Token);
                logger.LogInformation("Subscribed bot to chat for channel: {channelName}", account.BroadcasterId);
                
                logger.LogDebug("Sending request to emote api to preload emotes for channel: {broadcasterId}", account.BroadcasterId);
                //AllChannelEmotes?broadcasterId={broadcasterChannelId}
                await httpService.PostAsync(
                    $"{_startupSettings.EmoteApiUrl}{_startupSettings.EmoteChannelUri}?broadcasterId={account.BroadcasterId}",
                    null, null, null, null, cts.Token);
                logger.LogDebug("Emote api emotes preload request sent successfully for channel: {broadcasterId}", account.BroadcasterId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing to active channels on startup.");
        }
    }
}