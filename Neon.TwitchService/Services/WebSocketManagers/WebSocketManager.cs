using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Kafka;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Kafka;
using Neon.Core.Services.Twitch.Authentication;
using Neon.TwitchService.Models;
using Neon.TwitchService.Models.Kafka;
using Neon.TwitchService.Services.WebSockets;
using Newtonsoft.Json;

namespace Neon.TwitchService.Services.WebSocketManagers;

public class WebSocketManager(ILogger<WebSocketManager> logger, IOptions<BaseKafkaConfig> baseKafkaSettings, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IOAuthService oAuthService, IOptions<NeonSettings> twitchAppSettings) : IWebSocketManager
{
    private readonly BaseKafkaConfig _baseKafkaConfig = baseKafkaSettings.Value ?? throw new ArgumentNullException(nameof(baseKafkaSettings));
    private readonly NeonSettings _twitchAppSettings = twitchAppSettings.Value ?? throw new ArgumentNullException(nameof(twitchAppSettings));
    
    //TODO: maybe dont put the kafka/oauth service in the constructor long term

    //additionally holds neon bot for sending chat messages
    //channel name : iwebsocketservice
    private readonly Dictionary<string, IWebSocketService> _webSocketServices = [];

    public async Task Subscribe(string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterName))
            return;

        if (_webSocketServices.TryGetValue(broadcasterName, out _))
        {
            logger.LogDebug("Already subscribed to {broadcasterName}", broadcasterName);
            return;
        }

        logger.LogDebug("Subscribing to {broadcasterName}", broadcasterName);

        using var scope = serviceScopeFactory.CreateScope();

        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();

        _webSocketServices.Add(broadcasterName, wsService);

        await wsService.ConnectAsync(async twitchMessage =>
        {
            //logger.LogDebug("{channel} | {type} | {user} : {chatMessage}", twitchMessage?.Payload?.Event?.BroadcasterUserName, twitchMessage?.Payload?.Event?.MessageType, twitchMessage?.Payload?.Event?.ChatterUserName, twitchMessage?.Payload?.Event?.TwitchMessage?.Text);
            
            await kafkaService.ProduceAsync(new KafkaProducerConfig
            {
                Topic = "twitch-channel-events",
                TargetPartition = "0",
                BootstrapServers = _baseKafkaConfig.BootstrapServers
            }, JsonConvert.SerializeObject(twitchMessage), null, ct);
        }, ct);

        //use oauth to connect to broadcaster channel to start receiving events from twitch chat
        //TODO: dont do this here :)
        var twitchDbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();
        var broadcasterAccount = await twitchDbService.GetTwitchAccountByBroadcasterName(broadcasterName, ct);
        var appAccount = await twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (broadcasterAccount is null || broadcasterAccount.TwitchAccountAuth is null || appAccount is null)
            throw new Exception("Broadcaster account is null, broadcaster account auth is null, or app account is null!");

        var appTwitchAccount = new NeonTwitchBotSettings
        {
            Username = appAccount.AppName,
            ClientId = appAccount.ClientId,
            ClientSecret = appAccount.ClientSecret,
            AccessToken = appAccount.AccessToken,
            RedirectUri = appAccount.RedirectUri
        };

        wsService.SetNeonTwitchBotSettings(appTwitchAccount);

        var oAuthValidation = await oAuthService.ValidateOAuthToken(broadcasterAccount.TwitchAccountAuth.AccessToken, ct);

        //user auth failed, need to re-auth
        if (oAuthValidation is null)
        {
            logger.LogDebug("Access token needs refreshed or is invalid for account: {broadcasterName}", broadcasterName);
            var oAuthResp = await oAuthService.GetUserAuthTokenFromRefresh(appAccount.ClientId, appAccount.ClientSecret, broadcasterAccount.TwitchAccountAuth.RefreshToken, ct);

            if (oAuthResp is null || string.IsNullOrEmpty(oAuthResp.AccessToken))
            {
                logger.LogError("Failed to refresh access token for account: {broadcasterName}", broadcasterName);
                return;
            }

            broadcasterAccount.TwitchAccountAuth.AccessToken = oAuthResp.AccessToken;

            //update db with new access token
            _ = await twitchDbService.UpsertTwitchAccountAsync(broadcasterAccount, ct);
        }
        
        logger.LogDebug("User auth token is now valid for account: {broadcasterName}", broadcasterName);

        //reset the broadcaster account to ensure we have the latest data and decrypted access token info
        broadcasterAccount = await twitchDbService.GetTwitchAccountByBroadcasterName(broadcasterName, ct);
        
        if (broadcasterAccount is null || broadcasterAccount.TwitchAccountAuth is null)
        {
            logger.LogError("Broadcaster account or auth is null for account: {broadcasterName}", broadcasterName);
            throw new Exception("Broadcaster account is null or broadcaster account auth is null!");
        }
        
        var dbSubscriptions =
            broadcasterAccount.TwitchAccountScopes?.Where(s => s.AuthorizationScope is not null)
                .Select(s => s.AuthorizationScope!)
                .SelectMany(s => s.AuthorizationScopeSubscriptionTypes)
                .Select(s => s.SubscriptionType!)
                .DistinctBy(s => new { s.Name, s.Version }).ToList();
        
        var subscriptions = new List<SubscriptionType>();
        
        var defaultSubscriptions = await twitchDbService.GetDefaultSubscriptionsAsync(ct);

        defaultSubscriptions?.ForEach(s =>
        {
            var subscriptionType = new SubscriptionType
            {
                Name = s.Name,
                Version = s.Version
            };
            
            subscriptions.Add(subscriptionType);
        });
        
        dbSubscriptions?.ForEach(s =>
        {
            var subscriptionType = new SubscriptionType
            {
                Name = s.Name,
                Version = s.Version
            };
            
            subscriptions.Add(subscriptionType);
        });
        
        //now we have a valid access token for the person we're about to subscribe to, so call subscribe on the ws
        await wsService.SubscribeChannelAsync(broadcasterAccount.BroadcasterId, broadcasterAccount.TwitchAccountAuth.AccessToken, subscriptions, ct);
    }

    public async Task SubscribeUserToChat(string? userName, string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(broadcasterName))
            return;

        logger.LogDebug("Subscribing to chat {broadcasterName} using username of {userName}", broadcasterName, userName);

        using var scope = serviceScopeFactory.CreateScope();

        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();

        if (!_webSocketServices.TryGetValue(userName, out var existingWsService))
            _webSocketServices.Add(userName, wsService);

        await wsService.ConnectAsync(async twitchMessage =>
        {
            //logger.LogDebug("{channel} | {type} | {user} : {chatMessage}", twitchMessage?.Payload?.Event?.BroadcasterUserName, twitchMessage?.Payload?.Event?.MessageType, twitchMessage?.Payload?.Event?.ChatterUserName, twitchMessage?.Payload?.Event?.TwitchMessage?.Text);

            await kafkaService.ProduceAsync(new KafkaProducerConfig
            {
                Topic = "twitch-channel-chats",
                TargetPartition = "0",
                BootstrapServers = _baseKafkaConfig.BootstrapServers
            }, JsonConvert.SerializeObject(twitchMessage), null, ct);
        }, ct);

        //use oauth to connect to broadcaster channel to start receiving events from twitch chat
        //TODO: dont do this here :)
        var twitchDbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();
        var broadcasterAccount = await twitchDbService.GetTwitchAccountByBroadcasterName(broadcasterName, ct);
        var appAccount = await twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);
        var userAccount = await twitchDbService.GetTwitchAccountByBroadcasterName(userName, ct);

        if (appAccount is null || userAccount is null || userAccount.TwitchAccountAuth is null || broadcasterAccount is null || broadcasterAccount.TwitchAccountAuth is null)
        {
            logger.LogError("Unable to find app account or user account. AppAccount: {appAccount}, UserAccount: {userAccount}, BroadcasterAccount: {broadcasterAccount}", appAccount?.AppName, userAccount?.LoginName, broadcasterAccount?.LoginName);
            throw new Exception("App account is null, user account is null, or broadcaster account is null!");
        }

        var appTwitchAccount = new NeonTwitchBotSettings
        {
            Username = appAccount.AppName,
            ClientId = appAccount.ClientId,
            ClientSecret = appAccount.ClientSecret,
            AccessToken = appAccount.AccessToken,
            RedirectUri = appAccount.RedirectUri
        };

        wsService.SetNeonTwitchBotSettings(appTwitchAccount);

        var oAuthValidation = await oAuthService.ValidateOAuthToken(userAccount.TwitchAccountAuth.AccessToken, ct);

        //app auth failed, need to re-auth
        if (oAuthValidation is null)
        {
            logger.LogDebug("Access token needs refreshed or is invalid for account: {loginName}", userAccount.LoginName);
            var oAuthResp = await oAuthService.GetUserAuthTokenFromRefresh(appAccount.ClientId, appAccount.ClientSecret, userAccount.TwitchAccountAuth.RefreshToken, ct);

            if (oAuthResp is null || string.IsNullOrEmpty(oAuthResp.AccessToken))
            {
                logger.LogError("Failed to refresh access token for account: {loginName}", userAccount.LoginName);
                return;
            }

            userAccount.TwitchAccountAuth.AccessToken = oAuthResp.AccessToken;

            //update db with new access token
            _ = await twitchDbService.UpdateTwitchAccountAuthAsync(userAccount.BroadcasterId, userAccount.TwitchAccountAuth.AccessToken, ct);
        }

        var dbSubscriptions =
            userAccount.TwitchAccountScopes?.Where(s => s.AuthorizationScope is not null)
                .Select(s => s.AuthorizationScope!)
                .SelectMany(s => s.AuthorizationScopeSubscriptionTypes)
                .Select(s => s.SubscriptionType!)
                .DistinctBy(s => new { s.Name, s.Version }).ToList();

        var subscriptions = new List<SubscriptionType>();
        
        dbSubscriptions?.ForEach(s =>
        {
            var subscriptionType = new SubscriptionType
            {
                Name = s.Name,
                Version = s.Version
            };
            
            subscriptions.Add(subscriptionType);
        });
        
        logger.LogDebug("Subscribing bot to chat for {broadcasterName}", broadcasterAccount.BroadcasterId);
        await wsService.SubscribeChannelChatAsync(broadcasterAccount.BroadcasterId, userAccount.BroadcasterId, userAccount.TwitchAccountAuth.AccessToken, subscriptions, ct);
    }

    public async Task Unsubscribe(string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterName))
            return;

        if (!_webSocketServices.TryGetValue(broadcasterName, out var webSocketService))
        {
            logger.LogDebug("Unable to find connection for given user to close. BroadcasterName: {broadcasterName}", broadcasterName);
            return;
        }

        logger.LogDebug("Unsubscribing from {broadcasterName}", broadcasterName);
        await webSocketService.DisconnectAsync(ct);

        _webSocketServices.Remove(broadcasterName);

        logger.LogDebug("Unsubscribed from {broadcasterName}", broadcasterName);
    }
}
