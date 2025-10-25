using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Twitch;
using Neon.Core.Models.Twitch.EventSub;
using Neon.Core.Services.Http;
using Neon.TwitchService.Events;
using Neon.TwitchService.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neon.TwitchService.Services.WebSockets;

public class WebSocketService(ILogger<WebSocketService> logger, IOptions<TwitchSettings> twitchSettings, IHttpService httpService) : IWebSocketService
{
    private readonly TwitchSettings _twitchSettings = twitchSettings.Value ?? throw new ArgumentNullException(nameof(_twitchSettings));
    
    private NeonTwitchBotSettings? _botSettings;
    public void SetNeonTwitchBotSettings(NeonTwitchBotSettings? botSettings) => _botSettings = botSettings;
    
    public string? GetSessionId() => _sessionId;
    private string? _sessionId;

    public bool? IsReconnectRequested() => _reconnectRequested;
    private bool _reconnectRequested;

    private ClientWebSocket? _client;
    
    public bool IsConnected() => WsConnected;
    private bool WsConnected => (_client?.State ?? WebSocketState.None) == WebSocketState.Open;

    //event registration
    public event EventHandler<SessionReconnectEventArgs>? OnReconnectEvent;
    public event EventHandler<RevocationEventArgs>? OnRevocationEvent;
    public event EventHandler<NotificationEventArgs>? OnNotificationEvent;
    public event EventHandler<WebsocketClosedEventArgs>? OnWebsocketClosedEvent;
    
    public async Task ConnectAsync(string? wsUrl, CancellationToken ct = default)
    {
        if (_client is not null && WsConnected)
        {
            logger.LogWarning("Client is already connected");
            return;
        }

        var url = string.IsNullOrEmpty(wsUrl) ? _twitchSettings.EventSubUrl : wsUrl;

        if (string.IsNullOrEmpty(url))
        {
            logger.LogError("Twitch settings is missing event sub url for websocket connection or direct ws connection url is null. WsUrl: {wsUrl} | EventSubUrl: {url}", wsUrl, _twitchSettings.EventSubUrl);
            ArgumentNullException.ThrowIfNull(url, "WebSocketUrl");
        }

        logger.LogInformation("Connecting websocket at {time}", DateTime.UtcNow);
        _client = new ClientWebSocket();
        
        await _client.ConnectAsync(new Uri(url), ct);

        logger.LogInformation("Websocket connected at {time}. Hash: {hash}", DateTime.UtcNow, GetHashCode());

        _ = Task.Run(async () => await ListenAsync(ct), ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_client is null || !WsConnected)
        {
            logger.LogWarning("Client is already disconnected");
            return;
        }

        logger.LogInformation("Disconnecting websocket session {session} at {time}", _sessionId, DateTime.UtcNow);

        try
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", ct);
            _client.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disconnecting websocket");
        }
    }
    
    private async Task ListenAsync(CancellationToken ct = default)
    {
        var buffer = new byte[1024 * 8];
        var segment = new ArraySegment<byte>(buffer);

        while (WsConnected && !ct.IsCancellationRequested)
        {
            var result = await _client!.ReceiveAsync(segment, ct);

            if (result.MessageType == WebSocketMessageType.Close || _client.State == WebSocketState.CloseReceived || _client.State == WebSocketState.CloseSent || _client.State == WebSocketState.Closed || _client.State == WebSocketState.Aborted)
            {
                logger.LogWarning("Websocket connection closed. SessionId: {session}. Reason: {reason}", _sessionId, result.CloseStatusDescription);
                OnWebsocketClosed(_sessionId, result.CloseStatusDescription);
                break;
            }

            //drop bytes that are 0 and only get the value back from the string itself
            var msg = Encoding.UTF8.GetString(buffer.Where(s => s != 0).ToArray());
            HandleMessage(msg);
            Array.Clear(buffer, 0, buffer.Length);
        }

        logger.LogDebug("Websocket listen async method has exited the while loop, the connection has been closed.");
    }

    private void HandleMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        
        var messageType = JObject.Parse(message).SelectToken("metadata.message_type")?.ToString();
        
        // if (!string.IsNullOrEmpty(messageType) && !messageType.Equals("notification"))
        //     logger.LogDebug("Received ws message type: {messageType} | SessionId: {sessionId}", messageType, SessionId);
        
        if (!string.IsNullOrEmpty(messageType) && messageType.Equals("session_keepalive"))
            return;

        logger.LogDebug("Received ws message type: {messageType} | SessionId: {sessionId}", messageType, _sessionId);
        
        //_logger.LogDebug("Received message from twitch: {message}", message);
        Message? twitchMessage = null;
        try
        {
            twitchMessage = JsonConvert.DeserializeObject<Message>(message, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deserializing message from twitch: {message}", message);
        }

        //need to intercept session id from welcome message
        var wsSessionId = twitchMessage?.Payload?.Session?.Id;
        if (!string.IsNullOrEmpty(wsSessionId) && wsSessionId != _sessionId)
        {
            if (string.IsNullOrEmpty(_sessionId))
                logger.LogDebug("First time receiving session id from twitch websocket: {sessionId} | Hash: {hash}", _sessionId, GetHashCode());
            
            logger.LogDebug("Received new session id from twitch websocket: {sessionId} | Hash: {hash}", wsSessionId, GetHashCode());
            _sessionId = wsSessionId;
        }
        
        //handle reconnect requests from twitch
        if (!string.IsNullOrEmpty(messageType) && messageType.Equals("session_reconnect"))
        {
            logger.LogInformation("Received ws message for reconnect on session {sessionId} | Hash: {hash}", wsSessionId, GetHashCode());
            _reconnectRequested = true;
            OnReconnectRequested(twitchMessage?.Payload?.Session);
            return;
        }
        
        //handle ws revocations from twitch
        if (!string.IsNullOrEmpty(messageType) && messageType.Equals("revocation"))
        {
            logger.LogInformation("Received ws revocation message on session {sessionId}", _sessionId);
            OnRevocation(twitchMessage?.Payload?.Subscription);
            return;
        }

        if (!string.IsNullOrEmpty(messageType) && messageType.Equals("session_welcome"))
            return;

        //clientwebsocket should handle the ping/pong messages
        OnNotificationReceived(twitchMessage);
    }
    
    public async Task SubscribeChannelChatAsync(string? twitchChannelId, string? userId, string? accessToken, List<SubscriptionType>? subscriptionTypes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(_botSettings, nameof(_botSettings));

        if (string.IsNullOrEmpty(_botSettings.AccessToken) || string.IsNullOrEmpty(_botSettings.ClientId))
        {
            logger.LogError("Bot settings are missing access token or client id. Unable to subscribe to channel");
            ArgumentException.ThrowIfNullOrEmpty(_botSettings.AccessToken, nameof(_botSettings.AccessToken));
            ArgumentException.ThrowIfNullOrEmpty(_botSettings.ClientId, nameof(_botSettings.ClientId));
        }

        if (string.IsNullOrEmpty(twitchChannelId))
        {
            logger.LogWarning("TwitchChannelId is null or empty. Skipping subscription");
            return;
        }

        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("UserId is null or empty. Can't subscribe to channel chat without defining the user to connect as!");
            return;
        }

        if (subscriptionTypes is null || subscriptionTypes.Count == 0)
        {
            logger.LogWarning("Subscription type list is empty. Unable to subscribe to channel chat");
            return;
        }

        var authHeader = new AuthenticationHeaderValue("Bearer", accessToken);
        var headers = new Dictionary<string, string> 
        {
            { "Client-Id", _botSettings.ClientId } 
        };

        logger.LogDebug("Attempting to subscribe to total of {count} events for channel {channel}", subscriptionTypes.Count, twitchChannelId);
        
        foreach (var subscription in subscriptionTypes)
        {
            logger.LogDebug("Subscribing to channel event: {subscriptionName} | Version: {version} | Channel: {channel}", subscription.Name, subscription.Version, twitchChannelId);
            
            var message = new Message
            {
                Payload = new Payload
                {
                    Subscription = new Subscription
                    {
                        Type = subscription.Name,
                        Version = subscription.Version,
                        Condition = new Condition
                        {
                            BroadcasterUserId = twitchChannelId,
                            UserId = userId
                        },
                        Transport = new Transport
                        {
                            Method = "websocket",
                            SessionId = _sessionId
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(message.Payload.Subscription, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var resp = await httpService.PostAsync(_twitchSettings.EventSubscriptionUrl, content, MediaTypeNames.Application.Json, authHeader, headers, ct);

            if (resp is null || !resp.IsSuccessStatusCode)
            {
                var tRespContent = resp is null ? "no response" : await resp.Content.ReadAsStringAsync(ct);
                logger.LogDebug("Failed to subscribe to channel chat event {event}. StatusCode: {code} | Response: {response}", $"{subscription.Name}:{subscription.Version}", resp?.StatusCode, tRespContent);
                continue;
            }
            
            var respContent = await resp.Content.ReadAsStringAsync(ct);

            logger.LogInformation("Response from subscription: {response}", respContent);
        }
    }

    public async Task SubscribeChannelAsync(string? channel, string? accessToken, List<SubscriptionType>? subscriptionTypes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(_botSettings, nameof(_botSettings));

        if (string.IsNullOrEmpty(_botSettings.AccessToken) || string.IsNullOrEmpty(_botSettings.ClientId))
        {
            logger.LogError("Bot settings are missing access token or client id. Unable to subscribe to channel");
            ArgumentException.ThrowIfNullOrEmpty(_botSettings.AccessToken, nameof(_botSettings.AccessToken));
            ArgumentException.ThrowIfNullOrEmpty(_botSettings.ClientId, nameof(_botSettings.ClientId));
        }

        if (string.IsNullOrEmpty(channel))
        {
            logger.LogWarning("Channel is null or empty. Skipping subscription");
            return;
        }

        if (subscriptionTypes is null || subscriptionTypes.Count == 0)
        {
            logger.LogWarning("Subscription type list is empty. Unable to subscribe to channel events");
            return;
        }
        
        var authHeader = new AuthenticationHeaderValue("Bearer", accessToken);
        var headers = new Dictionary<string, string>
        {
            { "Client-Id", _botSettings.ClientId }
        };

        logger.LogDebug("Attempting to subscribe to total of {count} events for channel {channel}", subscriptionTypes.Count, channel);
        
        foreach (var subscription in subscriptionTypes)
        {
            if (string.IsNullOrEmpty(subscription.Name) || string.IsNullOrEmpty(subscription.Version))
                continue;
            
            logger.LogDebug("Subscribing to channel event: {subscriptionName} | Version: {version} | Channel: {channel}", subscription.Name, subscription.Version, channel);
            
            var message = new Message
            {
                Payload = new Payload
                {
                    Subscription = new Subscription
                    {
                        Type = subscription.Name,
                        Version = subscription.Version,
                        Condition = new Condition
                        {
                            BroadcasterUserId = channel,
                            ModeratorUserId = subscription.Name.Equals("channel.follow") ? channel : null,
                            UserId = subscription.Name.StartsWith("channel.chat") ? channel : null
                        },
                        Transport = new Transport
                        {
                            Method = "websocket",
                            SessionId = _sessionId
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(message.Payload.Subscription, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var resp = await httpService.PostAsync(_twitchSettings.EventSubscriptionUrl, content, MediaTypeNames.Application.Json, authHeader, headers, ct);

            if (resp is null || !resp.IsSuccessStatusCode)
            {
                var tRespContent = resp is null ? "no response" : await resp.Content.ReadAsStringAsync(ct);
                logger.LogDebug("Failed to subscribe to channel event {event}. StatusCode: {code} | Response: {response}", $"{subscription.Name}:{subscription.Version}", resp?.StatusCode, tRespContent);
                continue;
            }
            
            var respContent = await resp.Content.ReadAsStringAsync(ct);

            logger.LogInformation("Response from subscription: {response}", respContent);
        }
    }
    
    private void OnReconnectRequested(Session? session)
    {
        var args = new SessionReconnectEventArgs
        {
            Session = session,
            EventDate = DateTime.UtcNow
        };

        var handler = OnReconnectEvent;
        handler?.Invoke(this, args);
    }
    
    private void OnWebsocketClosed(string? sessionId, string? reason)
    {
        var args = new WebsocketClosedEventArgs
        {
            SessionId = sessionId,
            Reason = reason,
            EventDate = DateTime.UtcNow
        };

        var handler = OnWebsocketClosedEvent;
        handler?.Invoke(this, args);
    }
    
    private void OnRevocation(Subscription? subscription)
    {
        var args = new RevocationEventArgs
        {
            Subscription = subscription,
            EventDate = DateTime.UtcNow
        };

        var handler = OnRevocationEvent;
        handler?.Invoke(this, args);
    }
    
    private void OnNotificationReceived(Message? message)
    {
        var args = new NotificationEventArgs
        {
            Message = message,
            EventDate = DateTime.UtcNow
        };

        var handler = OnNotificationEvent;
        handler?.Invoke(this, args);
    }
}
