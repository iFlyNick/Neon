using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Neon.TwitchService.Models;
using Neon.Core.Models.Twitch.EventSub;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neon.TwitchService.Services.WebSockets;

public class WebSocketService(ILogger<WebSocketService> logger, IOptions<TwitchSettings> twitchSettings, IHttpService httpService) : IWebSocketService
{
    private readonly TwitchSettings _twitchSettings = twitchSettings.Value ?? throw new ArgumentNullException(nameof(_twitchSettings));

    private readonly List<string> _skippableMessages = [ "session_welcome" ];

    private NeonTwitchBotSettings? _botSettings;

    private Dictionary<string, List<SubscriptionType>> Subscriptions = [];
    private string? SessionId;
    private int _totalWebsocketCost = 0;
    private int _maxWebsocketCost = 300;

    private ClientWebSocket? Client;

    public void SetNeonTwitchBotSettings(NeonTwitchBotSettings? botSettings) => _botSettings = botSettings;

    public bool IsConnected() => _isConnected;
    private bool _isConnected => (Client?.State ?? WebSocketState.None) == WebSocketState.Open;

    public async Task ConnectAsync(Func<Message?, Task>? callback, CancellationToken ct = default)
    {
        if (callback is null)
        {
            logger.LogError("Callback is null, unable to send messages back from websocket when received.");
            ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        }

        if (Client is not null)
        {
            if (_isConnected)
            {
                logger.LogWarning("Client is already connected");
                return;
            }

            logger.LogInformation("Reconnecting websocket at {time}", DateTime.UtcNow);
            await ReconnectAsync(callback, ct);
        }

        if (string.IsNullOrEmpty(_twitchSettings.EventSubUrl))
        {
            logger.LogError("Twitch settings is missing event sub url for websocket connection. EventSubUrl: {url}", _twitchSettings.EventSubUrl);
            ArgumentNullException.ThrowIfNull(_twitchSettings.EventSubUrl, nameof(_twitchSettings.EventSubUrl));
        }

        logger.LogInformation("Connecting websocket at {time}", DateTime.UtcNow);
        Client = new ClientWebSocket();

        await Client.ConnectAsync(new Uri(_twitchSettings.EventSubUrl), ct);

        logger.LogInformation("Websocket connected at {time}. Hash: {hash}", DateTime.UtcNow, GetHashCode());

        _ = Task.Run(async () => await ListenAsync(callback, ct), ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (Client is null)
        {
            logger.LogWarning("Client is already disconnected");
            return;
        }

        if (!_isConnected)
        {
            logger.LogWarning("Client is already disconnected");
            return;
        }

        logger.LogInformation("Disconnecting websocket at {time}", DateTime.UtcNow);

        try
        {
            await Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disconnecting websocket");
        }
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

        foreach (var subscription in subscriptionTypes)
        {
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
                            SessionId = SessionId
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(message.Payload.Subscription, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var resp = await httpService.PostAsync(_twitchSettings.EventSubscriptionUrl, content, MediaTypeNames.Application.Json, authHeader, headers, ct);

            if (resp is null || !resp.IsSuccessStatusCode)
            {
                logger.LogDebug("Failed to subscribe to channel chat. Response: {response}", resp?.StatusCode);
                return;
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

        // var subscriptions = new List<(string, string)>
        // {
        //     ("channel.subscribe", "1"),
        //     ("channel.subscription.gift", "1"),
        //     ("channel.subscription.message", "1"),
        //     ("channel.channel_points_custom_reward_redemption.add", "1"),
        //     ("channel.channel_points_custom_reward_redemption.update", "1"),
        //     ("channel.ad_break.begin", "1"),
        //     ("channel.ban", "1"),
        //     ("channel.unban", "1"),
        //     ("channel.bits.use", "1"),
        //     ("channel.hype_train.begin", "1"),
        //     ("channel.hype_train.progress", "1"),
        //     ("channel.hype_train.end", "1"),
        //     ("channel.follow", "2"),
        //     ("channel.update", "2"),
        //     ("stream.online", "1"),
        //     ("stream.offline", "1")
        // };

        foreach (var subscription in subscriptionTypes)
        {
            if (string.IsNullOrEmpty(subscription.Name) || string.IsNullOrEmpty(subscription.Version))
                continue;
            
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
                            ModeratorUserId = subscription.Name.Equals("channel.follow") ? channel : null
                        },
                        Transport = new Transport
                        {
                            Method = "websocket",
                            SessionId = SessionId
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(message.Payload.Subscription, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var resp = await httpService.PostAsync(_twitchSettings.EventSubscriptionUrl, content, MediaTypeNames.Application.Json, authHeader, headers, ct);

            if (resp is null || !resp.IsSuccessStatusCode)
            {
                logger.LogDebug("Failed to subscribe to channel events. Response: {response}", resp?.StatusCode);
                return;
            }
            
            var respContent = await resp.Content.ReadAsStringAsync(ct);

            logger.LogInformation("Response from subscription: {response}", respContent);
        }
    }

    public async Task UnsubscribeChannelAsync(string? channel, List<SubscriptionType>? subscriptions, CancellationToken ct = default)
    {
        
    }

    public async Task SendMessageAsync(string? message, CancellationToken ct = default)
    {

    }

    private async Task ListenAsync(Func<Message?, Task> callback, CancellationToken ct = default)
    {
        var buffer = new byte[1024 * 8];
        var segment = new ArraySegment<byte>(buffer);

        while (_isConnected && !ct.IsCancellationRequested)
        {
            var result = await Client!.ReceiveAsync(segment, ct);

            if (result.MessageType == WebSocketMessageType.Close || Client.State == WebSocketState.CloseReceived || Client.State == WebSocketState.CloseSent || Client.State == WebSocketState.Closed || Client.State == WebSocketState.Aborted)
            {
                logger.LogWarning("Websocket connection closed. Reason: {reason}", result.CloseStatusDescription);
                await DisconnectAsync(ct);
                break;
            }

            //drop bytes that are 0 and only get the value back from the string itself
            var msg = Encoding.UTF8.GetString(buffer.Where(s => s != 0).ToArray());
            await HandleMessage(msg, callback, ct);
            Array.Clear(buffer, 0, buffer.Length);
        }

        logger.LogDebug("Websocket listen async method has exited the while loop, the connection will be closed.");
    }

    private async Task HandleMessage(string? message, Func<Message?, Task> callback, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(message))
            return;
        
        var messageType = JObject.Parse(message).SelectToken("metadata.message_type")?.ToString();
        
        // if (!string.IsNullOrEmpty(messageType) && !messageType.Equals("notification"))
        //     logger.LogDebug("Received ws message type: {messageType} | SessionId: {sessionId}", messageType, SessionId);
        
        if (!string.IsNullOrEmpty(messageType) && messageType.Equals("session_keepalive"))
            return;

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

        //_logger.LogDebug("{channel} | {type} | {user} : {chatMessage}", twitchMessage?.Payload?.Event?.BroadcasterUserName, twitchMessage?.Payload?.Event?.MessageType, twitchMessage?.Payload?.Event?.ChatterUserName, twitchMessage?.Payload?.Event?.TwitchMessage?.Text);

        //need to intercept session id from welcome message
        var wsSessionId = twitchMessage?.Payload?.Session?.Id;
        if (!string.IsNullOrEmpty(wsSessionId) && !wsSessionId.Equals(SessionId))
            SessionId = wsSessionId;

        if (!string.IsNullOrEmpty(messageType) && _skippableMessages.Contains(messageType))
            return;

        //clientwebsocket should handle the ping/pong messages
        await callback.Invoke(twitchMessage);
    }

    private async Task ReconnectAsync(Func<Message?, Task>? callback, CancellationToken ct = default)
    {
        await DisconnectAsync(ct);
        await ConnectAsync(callback, ct);
    }
}
