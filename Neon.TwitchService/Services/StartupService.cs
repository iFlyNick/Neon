using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.TwitchService.Services.WebSocketManagers;

namespace Neon.TwitchService.Services;

public class StartupService(ILogger<StartupService> logger, IWebSocketManager webSocketManager, IServiceScopeFactory serviceScopeFactory, IOptions<NeonSettings> neonSettings) : IHostedService
{
    private readonly NeonSettings _neonSettings = neonSettings.Value;

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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var scope = serviceScopeFactory.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();
            
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
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing to active channels on startup.");
        }
    }
}