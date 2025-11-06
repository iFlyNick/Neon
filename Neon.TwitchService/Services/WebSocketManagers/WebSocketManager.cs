using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Core.Models.Twitch.Helix.WebSockets;
using Neon.Core.Services.Kafka;
using Neon.Core.Services.Twitch.Authentication;
using Neon.Core.Services.Twitch.Helix;
using Neon.TwitchService.Events;
using Neon.TwitchService.Models;
using Neon.TwitchService.Models.Kafka;
using Neon.TwitchService.Services.WebSockets;
using Newtonsoft.Json;

namespace Neon.TwitchService.Services.WebSocketManagers;

public class WebSocketManager(ILogger<WebSocketManager> logger, IOptions<BaseKafkaConfig> baseKafkaSettings, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IOptions<NeonSettings> twitchAppSettings) : IWebSocketManager
{
    private readonly BaseKafkaConfig _baseKafkaConfig = baseKafkaSettings.Value ?? throw new ArgumentNullException(nameof(baseKafkaSettings));
    private readonly NeonSettings _twitchAppSettings = twitchAppSettings.Value ?? throw new ArgumentNullException(nameof(twitchAppSettings));

    private const string ProducerTopicEvents = "twitch-channel-events";
    private const string ProducerTopicChats = "twitch-channel-chats";
    
    //TODO: maybe dont put the kafka/oauth service in the constructor long term

    //additionally holds neon bot for sending chat messages
    //channel name : iwebsocketservice
    private readonly Dictionary<string, List<IWebSocketService>> _webSocketServices = [];
    public IEnumerable<IWebSocketService> GetWebSocketServices() => _webSocketServices.Values.SelectMany(s => s);

    private void OnSessionReconnectEvent(object? sender, SessionReconnectEventArgs e) => _ = HandleSessionReconnectEvent(sender, e);
    private void OnRevocationEvent(object? sender, RevocationEventArgs e) => _ = HandleRevocationEvent(sender, e);
    private void OnNotificationEvent(object? sender, NotificationEventArgs e) => _ = HandleNotificationEvent(sender, e);
    private void OnNotificationChatEvent(object? sender, NotificationEventArgs e) => _ = HandleChatNotificationEvent(sender, e);
    private void OnWebsocketClosedEvent(object? sender, WebsocketClosedEventArgs e) => _ = HandleWebsocketClosedEvent(sender, e);
    private void OnKeepAliveFailureEvent(object? sender, KeepAliveFailureEventArgs e) => _ = HandleKeepAliveFailureEvent(sender, e);

    private async Task HandleSessionReconnectEvent(object? sender, SessionReconnectEventArgs e)
    {
        try
        {
            if (e.Session is null || string.IsNullOrEmpty(e.Session.ReconnectUrl))
            {
                logger.LogWarning("OnSessionReconnectEvent: Session object or ReconnectUrl is null!");
                return;
            }
            
            if (sender is not IWebSocketService wsService)
            {
                logger.LogDebug("OnSessionReconnectEvent: Sender is not IWebSocketService!");
                return;
            }
            
            var oldSessionId = wsService.GetSessionId();
            var broadcasterId = wsService.GetBroadcasterId();
            var chatterId = wsService.GetChatterId();
            
            if (string.IsNullOrEmpty(broadcasterId))
            {
                logger.LogDebug("OnSessionReconnectEvent: Unable to find broadcaster id for websocket service with session id: {sessionId}", oldSessionId);
                return;
            }
            
            //create new ws service using the reconnect url for the given broadcaster
            var newWsService = await Resubscribe(e.Session.ReconnectUrl, broadcasterId, chatterId);

            if (newWsService is null)
            {
                logger.LogWarning("Failed to resubscribe to websocket for broadcaster: {broadcasterId}", broadcasterId);
                return;
            }
            
            //now that the new ws session has been created, the old one can be disconnected and removed
            //the old ws cant be closed until the new one has received the session welcome message from twitch. need to loop and wait for this to happen, or timeout after 30 seconds when it will no longer be allowed
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!timeoutCts.IsCancellationRequested)
            {
                if (newWsService.IsConnected() && !string.IsNullOrEmpty(newWsService.GetSessionId())) 
                    break;
                
                logger.LogDebug("Waiting for new websocket session to connect for broadcaster: {broadcasterId}", broadcasterId);
                await Task.Delay(500, timeoutCts.Token);
            }
            
            logger.LogDebug("New websocket session connected for broadcaster: {broadcasterId} | New SessionId: {newSessionId}", broadcasterId, newWsService.GetSessionId());
            
            if (newWsService.GetSessionId() == oldSessionId)
            {
                logger.LogDebug("***New websocket session id is the same as the old session id for broadcaster: {broadcasterId}. No need to disconnect old session.***", broadcasterId);
                
                if (_webSocketServices.TryGetValue(broadcasterId, out var wsList1))
                {
                    logger.LogDebug("Removing old websocket session from list for broadcaster: {broadcasterId} | Old SessionId: {oldSessionId}", broadcasterId, oldSessionId);
                    wsList1.RemoveAll(s => s.GetSessionId() == oldSessionId);
                    logger.LogDebug("Adding new websocket session to list for broadcaster: {broadcasterId} | New SessionId: {newSessionId}", broadcasterId, newWsService.GetSessionId());
                    wsList1.Add(newWsService);
                }
                else
                {
                    logger.LogDebug("Creating new websocket service list for broadcaster: {broadcasterId} | New SessionId: {newSessionId}", broadcasterId, newWsService.GetSessionId());
                    _webSocketServices[broadcasterId] = [newWsService];
                }
            }
            
            logger.LogDebug("Disconnecting old websocket session for broadcaster: {broadcasterId} | Old SessionId: {oldSessionId}", broadcasterId, oldSessionId);
            await wsService.DisconnectAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling session reconnect event!");
        }
    }
    
    private async Task HandleRevocationEvent(object? sender, RevocationEventArgs e)
    {
        try
        {
            logger.LogDebug("Revocation event raised. Sender: {sender} | Info: {revocationInfo}", sender, JsonConvert.SerializeObject(e.Subscription));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling revocation event!");
        }
    }
    
    private async Task HandleNotificationEvent(object? sender, NotificationEventArgs e)
    {
        try
        {
            var twitchMessage = e.Message;
            if (twitchMessage is null)
            {
                logger.LogDebug("OnNotificationEvent: TwitchMessage is null!");
                return;
            }
            
            await kafkaService.ProduceAsync(new ProducerConfig
                {
                    BootstrapServers = _baseKafkaConfig.BootstrapServers
                },
                ProducerTopicEvents,
                twitchMessage.Payload?.Event?.BroadcasterUserId,
                JsonConvert.SerializeObject(twitchMessage),
                null,
                CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling notification event!");
        }
    }
    
    private async Task HandleChatNotificationEvent(object? sender, NotificationEventArgs e)
    {
        try
        {
            var twitchMessage = e.Message;
            if (twitchMessage is null)
            {
                logger.LogDebug("OnNotificationEvent: TwitchMessage is null!");
                return;
            }
            
            await kafkaService.ProduceAsync(new ProducerConfig
                {
                    BootstrapServers = _baseKafkaConfig.BootstrapServers
                },
                ProducerTopicChats,
                twitchMessage.Payload?.Event?.BroadcasterUserId,
                JsonConvert.SerializeObject(twitchMessage),
                null,
                CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling notification event!");
        }
    }
    
    private async Task HandleWebsocketClosedEvent(object? sender, WebsocketClosedEventArgs e)
    {
        try
        {
            logger.LogDebug("Websocket closed event raised. SessionId: {sessionId} | Reason: {reason}", e.SessionId, e.Reason);

            if (sender is not IWebSocketService wsService)
            {
                logger.LogDebug("OnWebsocketClosedEvent: Sender is not IWebSocketService!");
                return;
            }
            
            var sessionId = wsService.GetSessionId();
            var broadcasterId = wsService.GetBroadcasterId();
            
            logger.LogDebug("Websocket closed event for broadcaster: {broadcasterId} | SessionId: {sessionId}. The ws will request a disconnect and the dictionary for the broadcaster will be updated or removed.", broadcasterId, sessionId);
            
            if (string.IsNullOrEmpty(broadcasterId))
            {
                logger.LogDebug("OnWebsocketClosedEvent: Unable to find broadcaster id for websocket service with session id: {sessionId}", sessionId);
                return;
            }
            
            //remove the ws service from the list for the broadcaster
            if (_webSocketServices.TryGetValue(broadcasterId, out var wsList))
            {
                wsList.RemoveAll(s => s.GetSessionId() == sessionId);
                await wsService.DisconnectAsync(CancellationToken.None);
                
                logger.LogDebug("Disconnected websocket session for broadcaster: {broadcasterId} | SessionId: {sessionId}", broadcasterId, sessionId);
                if (wsList.Count == 0)
                {
                    logger.LogDebug("No more websocket services for broadcaster: {broadcasterId}, removing from dictionary", broadcasterId);
                    _webSocketServices.Remove(broadcasterId);
                }
            }
            else
                logger.LogWarning("Unable to find websocket service list for broadcaster: {broadcasterId} when trying to remove session id: {sessionId}", broadcasterId, sessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling websocket closed event!");
        }
    }

    private async Task HandleKeepAliveFailureEvent(object? sender, KeepAliveFailureEventArgs e)
    {
        //keep alive failure event indicates the underlying ws has not sent a message within the configured time period. the existing ws should be disconnected and a new ws connection created as if it were starting fresh. this will include the base subscribe as well as the chat subscribe
        
        if (sender is not IWebSocketService wsService)
        {
            logger.LogDebug("OnSessionReconnectEvent: Sender is not IWebSocketService!");
            return;
        }
        
        var wsSessionId = wsService.GetSessionId();
        var broadcasterId = wsService.GetBroadcasterId();
        var chatterId = wsService.GetChatterId();
        
        //note - the broadcaster name here could be the BOT user if the wsChatUser is not null. the wsChannel is the CORRECT twitch broadcaster channel though. broadcaster name is used for the dictionary, but resub will need to use the right values!
        var wsOwner = _webSocketServices.FirstOrDefault(kvp => kvp.Value.Contains(wsService)).Key;
        
        if (_webSocketServices.TryGetValue(wsOwner, out var wsList))
        {
            wsList.RemoveAll(s => s.GetSessionId() == wsSessionId);
            await wsService.DisconnectAsync(CancellationToken.None);
                
            logger.LogDebug("Disconnected websocket session for broadcaster: {broadcasterName} | SessionId: {sessionId}", wsOwner, wsSessionId);
            
            if (wsList.Count == 0)
            {
                logger.LogDebug("No more websocket services for broadcaster: {broadcasterName}, removing from dictionary", wsOwner);
                _webSocketServices.Remove(wsOwner);
            }
        }
        else
            logger.LogWarning("Unable to find websocket service list for broadcaster: {broadcasterName}. Assuming all ws connections are now closed.", wsOwner);
        
        //start new subscriptions now using the broadcaster name
        //see note above about broadcaster name. from this point on to be clear, subscribe will use the channel, and subscribe user to chat will also use the channel
        //that channel is the channel that was alive on the old websocket and is not tied back to the bot should it have been the one to be disconnected
        using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        if (string.IsNullOrEmpty(chatterId))
            await Subscribe(broadcasterId, ct.Token);
        else 
            await SubscribeUserToChat(chatterId, broadcasterId, ct.Token);
    }

    private async Task<IWebSocketService?> Resubscribe(string? wsUrl, string? broadcasterId, string? chatterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(wsUrl) || string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogWarning("Resubscribe called with null or empty wsUrl or broadcasterId. wsUrl: {wsUrl}, broadcasterId: {broadcasterId}", wsUrl, broadcasterId);
            return null;
        }

        var scope = serviceScopeFactory.CreateScope();
        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
        
        wsService.OnReconnectEvent += OnSessionReconnectEvent;
        wsService.OnRevocationEvent += OnRevocationEvent;
        wsService.OnNotificationEvent += OnNotificationEvent;
        wsService.OnWebsocketClosedEvent += OnWebsocketClosedEvent;
        wsService.OnKeepAliveFailureEvent += OnKeepAliveFailureEvent;
        
        wsService.SetBroadcasterId(broadcasterId);
        wsService.SetChatterId(chatterId);
        
        await wsService.ConnectAsync(wsUrl, ct);
        
        return wsService;
    }
    
    public async Task Subscribe(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
            return;

        if (_webSocketServices.TryGetValue(broadcasterId, out _))
        {
            logger.LogDebug("Websocket already exists for broadcaster: {broadcasterId}", broadcasterId);
            return;
        }

        logger.LogDebug("Subscribing to {broadcasterId}", broadcasterId);

        using var scope = serviceScopeFactory.CreateScope();

        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
        
        wsService.OnReconnectEvent += OnSessionReconnectEvent;
        wsService.OnRevocationEvent += OnRevocationEvent;
        wsService.OnNotificationEvent += OnNotificationEvent;
        wsService.OnWebsocketClosedEvent += OnWebsocketClosedEvent;
        wsService.OnKeepAliveFailureEvent += OnKeepAliveFailureEvent;

        if (_webSocketServices.TryGetValue(broadcasterId, out var list))
            list.Add(wsService);
        else 
            _webSocketServices[broadcasterId] = [wsService];

        await wsService.ConnectAsync(null, ct);

        //use oauth to connect to broadcaster channel to start receiving events from twitch chat
        //TODO: dont do this here :)
        var twitchDbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();
        var userTokenService = scope.ServiceProvider.GetRequiredService<IUserTokenService>();
        
        var appAccount = await twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        ArgumentNullException.ThrowIfNull(appAccount);

        var appTwitchAccount = new NeonTwitchBotSettings
        {
            Username = appAccount.AppName,
            ClientId = appAccount.ClientId,
            ClientSecret = appAccount.ClientSecret,
            AccessToken = appAccount.AccessToken,
            RedirectUri = appAccount.RedirectUri
        };

        wsService.SetNeonTwitchBotSettings(appTwitchAccount);
        
        await userTokenService.EnsureUserTokenValidByBroadcasterId(broadcasterId, ct);

        //reset the broadcaster account to ensure we have the latest data and decrypted access token info
        var broadcasterAccount = await twitchDbService.GetTwitchAccountByBroadcasterIdAsync(broadcasterId, ct);
        
        if (broadcasterAccount is null || broadcasterAccount.TwitchAccountAuth is null)
        {
            logger.LogError("Broadcaster account or auth is null for account: {broadcasterId}", broadcasterId);
            throw new Exception("Broadcaster account is null or broadcaster account auth is null!");
        }
        
        var dbSubscriptions =
            broadcasterAccount.TwitchAccountScopes?.Where(s => s.AuthorizationScope is not null)
                .Select(s => s.AuthorizationScope!)
                .SelectMany(s => s.AuthorizationScopeSubscriptionTypes!)
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

    public async Task SubscribeUserToChat(string? chatterId, string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(chatterId) || string.IsNullOrEmpty(broadcasterId))
            return;

        logger.LogDebug("Subscribing to broadcasterId {broadcasterId} using chatterId of {chatterId}", broadcasterId, chatterId);

        using var scope = serviceScopeFactory.CreateScope();

        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
        
        wsService.OnReconnectEvent += OnSessionReconnectEvent;
        wsService.OnRevocationEvent += OnRevocationEvent;
        wsService.OnNotificationEvent += OnNotificationChatEvent;
        wsService.OnWebsocketClosedEvent += OnWebsocketClosedEvent;
        wsService.OnKeepAliveFailureEvent += OnKeepAliveFailureEvent;

        if (_webSocketServices.TryGetValue(chatterId, out var list))
            list.Add(wsService);
        else 
            _webSocketServices[chatterId] = [wsService];
        
        await wsService.ConnectAsync(null, ct);

        //use oauth to connect to broadcaster channel to start receiving events from twitch chat
        //TODO: dont do this here :)
        var twitchDbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();
        var userTokenService = scope.ServiceProvider.GetRequiredService<IUserTokenService>();
        
        var broadcasterAccount = await twitchDbService.GetTwitchAccountByBroadcasterIdAsync(broadcasterId, ct);
        var appAccount = await twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (appAccount is null || broadcasterAccount is null || broadcasterAccount.TwitchAccountAuth is null)
        {
            logger.LogError("Unable to find app account or broadcaster account. AppAccount: {appAccount}, BroadcasterAccount: {broadcasterAccount}", appAccount?.AppName, broadcasterAccount?.BroadcasterId);
            throw new Exception("App account is null or broadcaster account is null!");
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

        await userTokenService.EnsureUserTokenValidByBroadcasterId(chatterId, ct);
        
        var chatterAccount = await twitchDbService.GetTwitchAccountByBroadcasterIdAsync(chatterId, ct);
        ArgumentNullException.ThrowIfNull(chatterAccount);

        var dbSubscriptions =
            chatterAccount.TwitchAccountScopes?.Where(s => s.AuthorizationScope is not null)
                .Select(s => s.AuthorizationScope!)
                .SelectMany(s => s.AuthorizationScopeSubscriptionTypes!)
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
        
        logger.LogDebug("Subscribing chatterId {chatterId} to chat for broadcasterId {broadcasterName}", chatterId, broadcasterAccount.BroadcasterId);
        await wsService.SubscribeChannelChatAsync(broadcasterAccount.BroadcasterId, chatterAccount.BroadcasterId, chatterAccount.TwitchAccountAuth.AccessToken, subscriptions, ct);
    }

    public async Task Unsubscribe(string? broadcasterName, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        // if (string.IsNullOrEmpty(broadcasterName))
        //     return;
        //
        // if (!_webSocketServices.TryGetValue(broadcasterName, out var webSocketService))
        // {
        //     logger.LogDebug("Unable to find connection for given user to close. BroadcasterName: {broadcasterName}", broadcasterName);
        //     return;
        // }
        //
        // logger.LogDebug("Unsubscribing from {broadcasterName}", broadcasterName);
        // await webSocketService.DisconnectAsync(ct);
        //
        // _webSocketServices.Remove(broadcasterName);
        //
        // logger.LogDebug("Unsubscribed from {broadcasterName}", broadcasterName);
    }

    public async Task<List<WebSocketSubscription>?> GetSubscriptions(string? userAccessToken, string? sessionId, string? chatUserId, string? broadcasterUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userAccessToken);
        
        using var scope = serviceScopeFactory.CreateScope();

        var helixService = scope.ServiceProvider.GetRequiredService<IHelixService>();
        var subscriptions = await helixService.GetWebSocketSubscriptions(userAccessToken, ct);

        if (subscriptions is null || subscriptions.Count == 0)
        {
            logger.LogDebug("No websocket subscriptions found for sessionId: {sessionId}", sessionId);
            return null;
        }

        if (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(chatUserId) && string.IsNullOrEmpty(broadcasterUserId))
            return subscriptions;

        if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(chatUserId) && !string.IsNullOrEmpty(broadcasterUserId))
        {
            //return double filtered list
            var filteredBySessionAndChatUser = subscriptions.Where(s => s.Transport?.SessionId == sessionId && s.Condition?.UserId == chatUserId && s.Condition?.BroadcasterUserId == broadcasterUserId);
            
            return filteredBySessionAndChatUser.ToList();
        }
        
        //filter the list out to only include those that match the sessionId or chatUserId
        var retList = new List<WebSocketSubscription>();

        if (!string.IsNullOrEmpty(sessionId))
        {
            var filteredBySession = subscriptions.Where(s => s.Transport?.SessionId == sessionId).ToList();
            
            retList.AddRange(filteredBySession);
        }

        if (!string.IsNullOrEmpty(broadcasterUserId))
        {
            var filteredByChatUser = subscriptions.Where(s => s.Condition?.BroadcasterUserId == broadcasterUserId).ToList();
            retList.AddRange(filteredByChatUser);
        }
        
        if (!string.IsNullOrEmpty(chatUserId))
        {
            var filteredByChatUser = subscriptions.Where(s => s.Condition?.UserId == chatUserId).ToList();
            retList.AddRange(filteredByChatUser);
        }

        return retList.DistinctBy(s => s.Id).ToList();
    }
}
