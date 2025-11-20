using Neon.Core.Data.Twitch;
using Neon.StreamElementsService.Services.WebSocketManagers;

namespace Neon.StreamElementsService.Services;

public class StartupService(ILogger<StartupService> logger, IWebSocketManager webSocketManager, IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting streamelements websocket service...");
        await SubscribeAllActiveChannels();
    }

    public Task StopAsync(CancellationToken ct)
    {
        logger.LogInformation("Shutting down streamelements websocket service...");
        return Task.CompletedTask;
    }

    private async Task SubscribeAllActiveChannels()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var scope = serviceScopeFactory.CreateScope();
        var twitchDbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();

        var activeAccounts = await twitchDbService.GetAllAccountsWithStreamElementAuths(cts.Token);

        if (activeAccounts is null || activeAccounts.Count == 0)
        {
            logger.LogDebug("No active Twitch accounts with StreamElements auth found to subscribe.");
            return;
        }

        foreach (var activeAccount in activeAccounts)
        {
            logger.LogDebug("Subscribing to StreamElements channel {seChannelId} for Twitch broadcaster {broadcasterId}.", activeAccount.StreamElementsAuth?.StreamElementsChannel, activeAccount.BroadcasterId);
            
            await webSocketManager.Subscribe(activeAccount.BroadcasterId, activeAccount.StreamElementsAuth?.StreamElementsChannel, activeAccount.StreamElementsAuth?.JwtToken, cts.Token);
            
            logger.LogDebug("Subscribed to StreamElements channel {seChannelId} for Twitch broadcaster {broadcasterId}.", activeAccount.StreamElementsAuth?.StreamElementsChannel, activeAccount.BroadcasterId);
        }
    }
}