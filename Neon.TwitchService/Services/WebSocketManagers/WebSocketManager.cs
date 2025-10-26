using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Kafka;
using Neon.Core.Services.Twitch.Authentication;
using Neon.TwitchService.Events;
using Neon.TwitchService.Models;
using Neon.TwitchService.Models.Kafka;
using Neon.TwitchService.Services.WebSockets;
using Newtonsoft.Json;

namespace Neon.TwitchService.Services.WebSocketManagers;

public class WebSocketManager(ILogger<WebSocketManager> logger, IOptions<BaseKafkaConfig> baseKafkaSettings, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IOAuthService oAuthService, IOptions<NeonSettings> twitchAppSettings) : IWebSocketManager
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
            var broadcasterName = _webSocketServices.FirstOrDefault(kvp => kvp.Value.Contains(wsService)).Key;
            
            if (string.IsNullOrEmpty(broadcasterName))
            {
                logger.LogDebug("OnSessionReconnectEvent: Unable to find broadcaster name for websocket service with session id: {sessionId}", oldSessionId);
                return;
            }
            
            //create new ws service using the reconnect url for the given broadcaster
            var newWsService = await Resubscribe(e.Session.ReconnectUrl, broadcasterName);

            if (newWsService is null)
            {
                logger.LogWarning("Failed to resubscribe to websocket for broadcaster: {broadcasterName}", broadcasterName);
                return;
            }
            
            //now that the new ws session has been created, the old one can be disconnected and removed
            //the old ws cant be closed until the new one has received the session welcome message from twitch. need to loop and wait for this to happen, or timeout after 30 seconds when it will no longer be allowed
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!timeoutCts.IsCancellationRequested)
            {
                if (newWsService.IsConnected()) 
                    break;
                
                logger.LogDebug("Waiting for new websocket session to connect for broadcaster: {broadcasterName}", broadcasterName);
                await Task.Delay(500, timeoutCts.Token);
            }
            
            logger.LogDebug("New websocket session connected for broadcaster: {broadcasterName} | New SessionId: {newSessionId}", broadcasterName, newWsService.GetSessionId());

            //update the ws service list for the broadcaster
            if (_webSocketServices.TryGetValue(broadcasterName, out var wsList))
            {
                wsList.RemoveAll(s => s.GetSessionId() == oldSessionId);
                await wsService.DisconnectAsync(CancellationToken.None);
                
                logger.LogDebug("Disconnected old websocket session for broadcaster: {broadcasterName} | Old SessionId: {oldSessionId} | New SessionId: {newSessionId}", broadcasterName, oldSessionId, newWsService.GetSessionId());

                if (wsList.Count == 0)
                {
                    logger.LogDebug("No more websocket services for broadcaster: {broadcasterName}, removing from dictionary", broadcasterName);
                    _webSocketServices.Remove(broadcasterName);
                }
            }
            else
                logger.LogWarning("Unable to find websocket service list for broadcaster: {broadcasterName} when trying to remove old session id: {oldSessionId}", broadcasterName, oldSessionId);
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
            var broadcasterName = _webSocketServices.FirstOrDefault(kvp => kvp.Value.Contains(wsService)).Key;
            
            logger.LogDebug("Websocket closed event for broadcaster: {broadcasterName} | SessionId: {sessionId}. The ws will request a disconnect and the dictionary for the broadcaster will be updated or removed.", broadcasterName, sessionId);
            
            if (string.IsNullOrEmpty(broadcasterName))
            {
                logger.LogDebug("OnWebsocketClosedEvent: Unable to find broadcaster name for websocket service with session id: {sessionId}", sessionId);
                return;
            }
            
            //remove the ws service from the list for the broadcaster
            if (_webSocketServices.TryGetValue(broadcasterName, out var wsList))
            {
                wsList.RemoveAll(s => s.GetSessionId() == sessionId);
                await wsService.DisconnectAsync(CancellationToken.None);
                
                logger.LogDebug("Disconnected websocket session for broadcaster: {broadcasterName} | SessionId: {sessionId}", broadcasterName, sessionId);
                if (wsList.Count == 0)
                {
                    logger.LogDebug("No more websocket services for broadcaster: {broadcasterName}, removing from dictionary", broadcasterName);
                    _webSocketServices.Remove(broadcasterName);
                }
            }
            else
                logger.LogWarning("Unable to find websocket service list for broadcaster: {broadcasterName} when trying to remove session id: {sessionId}", broadcasterName, sessionId);
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
        var wsChatUser = wsService.GetChatUser();
        var broadcasterName = _webSocketServices.FirstOrDefault(kvp => kvp.Value.Contains(wsService)).Key;
        
        if (_webSocketServices.TryGetValue(broadcasterName, out var wsList))
        {
            wsList.RemoveAll(s => s.GetSessionId() == wsSessionId);
            await wsService.DisconnectAsync(CancellationToken.None);
                
            logger.LogDebug("Disconnected websocket session for broadcaster: {broadcasterName} | SessionId: {sessionId}", broadcasterName, wsSessionId);
            
            if (wsList.Count == 0)
            {
                logger.LogDebug("No more websocket services for broadcaster: {broadcasterName}, removing from dictionary", broadcasterName);
                _webSocketServices.Remove(broadcasterName);
            }
        }
        else
            logger.LogWarning("Unable to find websocket service list for broadcaster: {broadcasterName}. Assuming all ws connections are now closed.", broadcasterName);
        
        //start new subscriptions now using the broadcaster name
        using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        if (string.IsNullOrEmpty(wsChatUser))
            await Subscribe(broadcasterName, ct.Token);
        else 
            await SubscribeUserToChat(_twitchAppSettings.AppName, wsChatUser, ct.Token);
    }

    private async Task<IWebSocketService?> Resubscribe(string? wsUrl, string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(wsUrl) || string.IsNullOrEmpty(broadcasterName))
        {
            logger.LogWarning("Resubscribe called with null or empty wsUrl or broadcasterName. wsUrl: {wsUrl}, broadcasterName: {broadcasterName}", wsUrl, broadcasterName);
            return null;
        }

        var scope = serviceScopeFactory.CreateScope();
        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
        
        wsService.OnReconnectEvent += OnSessionReconnectEvent;
        wsService.OnRevocationEvent += OnRevocationEvent;
        wsService.OnNotificationEvent += OnNotificationEvent;
        wsService.OnWebsocketClosedEvent += OnWebsocketClosedEvent;
        wsService.OnKeepAliveFailureEvent += OnKeepAliveFailureEvent;
        
        if (_webSocketServices.TryGetValue(broadcasterName, out var list))
            list.Add(wsService);
        else 
            _webSocketServices[broadcasterName] = [wsService];
        
        await wsService.ConnectAsync(wsUrl, ct);
        
        return wsService;
    }
    
    public async Task Subscribe(string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterName))
            return;

        if (_webSocketServices.TryGetValue(broadcasterName, out _))
        {
            logger.LogDebug("Websocket already exists for broadcaster: {broadcasterName}", broadcasterName);
            return;
        }

        logger.LogDebug("Subscribing to {broadcasterName}", broadcasterName);

        using var scope = serviceScopeFactory.CreateScope();

        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
        
        wsService.OnReconnectEvent += OnSessionReconnectEvent;
        wsService.OnRevocationEvent += OnRevocationEvent;
        wsService.OnNotificationEvent += OnNotificationEvent;
        wsService.OnWebsocketClosedEvent += OnWebsocketClosedEvent;
        wsService.OnKeepAliveFailureEvent += OnKeepAliveFailureEvent;

        if (_webSocketServices.TryGetValue(broadcasterName, out var list))
            list.Add(wsService);
        else 
            _webSocketServices[broadcasterName] = [wsService];

        await wsService.ConnectAsync(null, ct);

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

    public async Task SubscribeUserToChat(string? userName, string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(broadcasterName))
            return;

        logger.LogDebug("Subscribing to chat {broadcasterName} using username of {userName}", broadcasterName, userName);

        using var scope = serviceScopeFactory.CreateScope();

        var wsService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
        
        wsService.OnReconnectEvent += OnSessionReconnectEvent;
        wsService.OnRevocationEvent += OnRevocationEvent;
        wsService.OnNotificationEvent += OnNotificationChatEvent;
        wsService.OnWebsocketClosedEvent += OnWebsocketClosedEvent;
        wsService.OnKeepAliveFailureEvent += OnKeepAliveFailureEvent;

        if (_webSocketServices.TryGetValue(userName, out var list))
            list.Add(wsService);
        else 
            _webSocketServices[userName] = [wsService];
        
        await wsService.ConnectAsync(null, ct);

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
        
        logger.LogDebug("Subscribing bot to chat for {broadcasterName}", broadcasterAccount.BroadcasterId);
        await wsService.SubscribeChannelChatAsync(broadcasterAccount.BroadcasterId, userAccount.BroadcasterId, userAccount.TwitchAccountAuth.AccessToken, subscriptions, ct);
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
}
