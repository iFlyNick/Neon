using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models.Kafka;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Kafka;
using Neon.Core.Services.Twitch.Authentication;
using Neon.TwitchService.Models.Kafka;
using Neon.TwitchService.Services.WebSockets;
using Newtonsoft.Json;

namespace Neon.TwitchService.Services.WebSocketManagers;

public class WebSocketManager(ILogger<WebSocketManager> logger, IOptions<BaseKafkaConfig> baseKafkaSettings, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IOAuthService oAuthService) : IWebSocketManager
{
    private readonly BaseKafkaConfig _baseKafkaConfig = baseKafkaSettings.Value ?? throw new ArgumentNullException(nameof(baseKafkaSettings));
    
    //TODO: maybe dont put the kafka/oauth service in the constructor long term

    //additionally holds neon bot for sending chat messages
    //channel name : iwebsocketservice
    private readonly Dictionary<string, IWebSocketService> _webSocketServices = [];

    public async Task Subscribe(string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterName))
            return;

        if (_webSocketServices.TryGetValue(broadcasterName, out var webSocketService))
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
            logger.LogDebug("{channel} | {type} | {user} : {chatMessage}", twitchMessage?.Payload?.Event?.BroadcasterUserName, twitchMessage?.Payload?.Event?.MessageType, twitchMessage?.Payload?.Event?.ChatterUserName, twitchMessage?.Payload?.Event?.TwitchMessage?.Text);
            
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
        var botAccount = await twitchDbService.GetBotAccountAsync("TheNeonBot", ct);

        if (broadcasterAccount is null || botAccount is null)
            throw new Exception("ruh roh");

        var appTwitchAccount = new NeonTwitchBotSettings
        {
            Username = botAccount.BotName,
            ClientId = botAccount.ClientId,
            ClientSecret = botAccount.ClientSecret,
            AccessToken = botAccount.AccessToken,
            RedirectUri = botAccount.RedirectUri,
            BroadcasterId = botAccount.TwitchBroadcasterId
        };

        wsService.SetNeonTwitchBotSettings(appTwitchAccount);

        var oAuthValidation = await oAuthService.ValidateOAuthToken(broadcasterAccount.AccessToken, ct);

        //user auth failed, need to reauth
        if (oAuthValidation is null)
        {
            logger.LogDebug("Access token needs refreshed or is invalid for account: {broadcasterName}", broadcasterName);
            var oAuthResp = await oAuthService.GetUserAuthTokenFromRefresh(botAccount.ClientId, botAccount.ClientSecret, broadcasterAccount.RefreshToken);

            if (oAuthResp is null || string.IsNullOrEmpty(oAuthResp.AccessToken))
            {
                logger.LogError("Failed to refresh access token for account: {broadcasterName}", broadcasterName);
                return;
            }

            broadcasterAccount.AccessToken = oAuthResp.AccessToken;

            //update db with new access token
            _ = await twitchDbService.UpsertTwitchAccountAsync(broadcasterAccount, ct);
        }

        //now we have a valid access token for the person we're about to subscribe to, so call subscribe on the ws
        await wsService.SubscribeChannelAsync(broadcasterAccount.BroadcasterId, broadcasterAccount.AccessToken, null, ct);
    }

    public async Task SubscribeBotToChat(string? botName, string? broadcasterName, string? overrideBroadcasterId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(botName) || (string.IsNullOrEmpty(broadcasterName) && string.IsNullOrEmpty(overrideBroadcasterId)))
            return;

        logger.LogDebug("Subscribing to chat: {broadcasterName}", broadcasterName);

        using var scope = serviceScopeFactory.CreateScope();

        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();

        if (!_webSocketServices.TryGetValue(botName, out var existingWsService))
            _webSocketServices.Add(botName, wsService);

        await wsService.ConnectAsync(async twitchMessage =>
        {
            logger.LogDebug("{channel} | {type} | {user} : {chatMessage}", twitchMessage?.Payload?.Event?.BroadcasterUserName, twitchMessage?.Payload?.Event?.MessageType, twitchMessage?.Payload?.Event?.ChatterUserName, twitchMessage?.Payload?.Event?.TwitchMessage?.Text);

            await kafkaService.ProduceAsync(new KafkaProducerConfig
            {
                Topic = "twitch-channel-chats",
                TargetPartition = "0",
                BootstrapServers = "localhost:9092"
            }, JsonConvert.SerializeObject(twitchMessage), null, ct);
        }, ct);

        //use oauth to connect to broadcaster channel to start receiving events from twitch chat
        //TODO: dont do this here :)
        var twitchDbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();
        var broadcasterAccount = await twitchDbService.GetTwitchAccountByBroadcasterName(broadcasterName, ct);
        var appAccount = await twitchDbService.GetBotAccountAsync(botName, ct);
        var botAccount = await twitchDbService.GetNeonBotTwitchAccountAsync(ct);

        if (appAccount is null || botAccount is null || (broadcasterAccount is null && string.IsNullOrEmpty(overrideBroadcasterId)))
            throw new Exception("ruh roh");

        var appTwitchAccount = new NeonTwitchBotSettings
        {
            Username = appAccount.BotName,
            ClientId = appAccount.ClientId,
            ClientSecret = appAccount.ClientSecret,
            AccessToken = appAccount.AccessToken,
            RedirectUri = appAccount.RedirectUri,
            BroadcasterId = appAccount.TwitchBroadcasterId
        };

        wsService.SetNeonTwitchBotSettings(appTwitchAccount);

        var oAuthValidation = await oAuthService.ValidateOAuthToken(botAccount.AccessToken, ct);

        //bot auth failed, need to reauth
        if (oAuthValidation is null)
        {
            logger.LogDebug("Access token needs refreshed or is invalid for account: {botName}", botAccount.BroadcasterId);
            var oAuthResp = await oAuthService.GetUserAuthTokenFromRefresh(appAccount.ClientId, appAccount.ClientSecret, botAccount.RefreshToken);

            if (oAuthResp is null || string.IsNullOrEmpty(oAuthResp.AccessToken))
            {
                logger.LogError("Failed to refresh access token for account: {botName}", botName);
                return;
            }

            botAccount.AccessToken = oAuthResp.AccessToken;

            //update db with new access token
            _ = await twitchDbService.UpdateTwitchAccountAuthAsync(botAccount.BroadcasterId, botAccount.AccessToken, ct);
        }

        logger.LogDebug("Subscribing bot to chat for {broadcasterName}", broadcasterName);
        await wsService.SubscribeChannelChatAsync(string.IsNullOrEmpty(overrideBroadcasterId) ? broadcasterAccount!.BroadcasterId : overrideBroadcasterId, botAccount.AccessToken, null, ct);
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
