using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.TwitchService.Services.WebSocketManagers;

namespace Neon.TwitchService.Services;

public class StartupService(ILogger<StartupService> logger, IWebSocketManager webSocketManager, ITwitchDbService dbService, IOptions<NeonSettings> neonSettings) : IStartupService
{
    private readonly NeonSettings _neonSettings = neonSettings.Value;
    
    public async Task SubscribeAllActiveChannels(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_neonSettings.AppName))
        {
            logger.LogCritical("AppName is null or empty. Cannot subscribe to channels.");
            return;
        }
        
        var subscribedAccounts = await dbService.GetAllSubscribedChannelAccounts(ct);
        if (subscribedAccounts is null || subscribedAccounts.Count == 0)
        {
            logger.LogInformation("No subscribed accounts found.");
            return;
        }
        
        foreach (var account in subscribedAccounts)
        {
            if (string.IsNullOrEmpty(account.BroadcasterId))
            {
                logger.LogWarning("Broadcaster id is null or empty for account: {account}", account);
                continue;
            }
            await webSocketManager.Subscribe(account.LoginName, ct);
            logger.LogInformation("Subscribed to channel: {channelName}", account.LoginName);
            
            //now connect the bot to the channel too
            await webSocketManager.SubscribeUserToChat(_neonSettings.AppName, account.LoginName, ct);
        }
    }
}