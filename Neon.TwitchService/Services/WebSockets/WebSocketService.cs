using Microsoft.Extensions.Options;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.TwitchService.Models;
using Neon.TwitchService.Models.Twitch;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;

namespace Neon.TwitchService.Services.WebSockets;

public class WebSocketService(ILogger<WebSocketService> logger, IOptions<TwitchSettings> twitchSettings, IHttpService httpService) : IWebSocketService
{
    private readonly ILogger<WebSocketService> _logger = logger;
    private readonly TwitchSettings _twitchSettings = twitchSettings.Value ?? throw new ArgumentNullException(nameof(_twitchSettings));
    private readonly IHttpService _httpService = httpService;

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
            _logger.LogError("Callback is null, unable to send messages back from websocket when received.");
            ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        }

        if (Client is not null)
        {
            if (_isConnected)
            {
                _logger.LogWarning("Client is already connected");
                return;
            }

            _logger.LogInformation("Reconnecting websocket at {time}", DateTime.UtcNow);
            await ReconnectAsync(callback, ct);
        }

        if (string.IsNullOrEmpty(_twitchSettings.EventSubUrl))
        {
            _logger.LogError("Twitch settings is missing event sub url for websocket connection. EventSubUrl: {url}", _twitchSettings.EventSubUrl);
            ArgumentNullException.ThrowIfNull(_twitchSettings.EventSubUrl, nameof(_twitchSettings.EventSubUrl));
        }

        _logger.LogInformation("Connecting websocket at {time}", DateTime.UtcNow);
        Client = new ClientWebSocket();

        await Client.ConnectAsync(new Uri(_twitchSettings.EventSubUrl), ct);

        _logger.LogInformation("Websocket connected at {time}. Hash: {hash}", DateTime.UtcNow, GetHashCode());

        _ = Task.Run(async () => await ListenAsync(callback, ct), ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (Client is null)
        {
            _logger.LogWarning("Client is already disconnected");
            return;
        }

        if (!_isConnected)
        {
            _logger.LogWarning("Client is already disconnected");
            return;
        }

        _logger.LogInformation("Disconnecting websocket at {time}", DateTime.UtcNow);

        try
        {
            await Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting websocket");
        }
    }

    public async Task SubscribeChannelChatAsync(string? channel, string? accessToken, List<SubscriptionType>? subscriptionsTypes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(_botSettings, nameof(_botSettings));

        if (string.IsNullOrEmpty(_botSettings.AccessToken) || string.IsNullOrEmpty(_botSettings.ClientId))
        {
            _logger.LogError("Bot settings are missing access token or client id. Unable to subscribe to channel");
            ArgumentNullException.ThrowIfNullOrEmpty(_botSettings.AccessToken, nameof(_botSettings.AccessToken));
            ArgumentNullException.ThrowIfNullOrEmpty(_botSettings.ClientId, nameof(_botSettings.ClientId));
        }

        if (string.IsNullOrEmpty(channel))
        {
            _logger.LogWarning("Channel is null or empty. Skipping subscription");
            return;
        }

        var authHeader = new AuthenticationHeaderValue("Bearer", accessToken);
        var headers = new Dictionary<string, string> 
        {
            { "Client-Id", _botSettings.ClientId } 
        };

        var subscriptions = new List<(string, string)>
        {
            { ("channel.chat.message", "1") },
            { ("channel.chat.notification", "1") },
            //{ ("channel.update", "2") },
            //{ ("stream.online", "1") },
            //{ ("stream.offline", "1") },
        };

        foreach (var subscription in subscriptions)
        {
            var message = new Message()
            {
                Payload = new()
                {
                    Subscription = new()
                    {
                        Type = subscription.Item1,
                        Version = subscription.Item2,
                        Condition = new()
                        {
                            BroadcasterUserId = channel,
                            UserId = _botSettings.BroadcasterId
                        },
                        Transport = new()
                        {
                            Method = "websocket",
                            SessionId = SessionId
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(message.Payload.Subscription, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var resp = await _httpService.PostAsync(_twitchSettings.EventSubscriptionUrl, content, MediaTypeNames.Application.Json, authHeader, headers, ct);

            var respContent = await resp?.Content?.ReadAsStringAsync();

            _logger.LogInformation("Response from subscription: {response}", respContent);
        }
    }

    public async Task SubscribeChannelAsync(string? channel, string? accessToken, List<SubscriptionType>? subscriptionTypes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(_botSettings, nameof(_botSettings));

        if (string.IsNullOrEmpty(_botSettings.AccessToken) || string.IsNullOrEmpty(_botSettings.ClientId))
        {
            _logger.LogError("Bot settings are missing access token or client id. Unable to subscribe to channel");
            ArgumentNullException.ThrowIfNullOrEmpty(_botSettings.AccessToken, nameof(_botSettings.AccessToken));
            ArgumentNullException.ThrowIfNullOrEmpty(_botSettings.ClientId, nameof(_botSettings.ClientId));
        }

        if (string.IsNullOrEmpty(channel))
        {
            _logger.LogWarning("Channel is null or empty. Skipping subscription");
            return;
        }

        var authHeader = new AuthenticationHeaderValue("Bearer", accessToken);
        var headers = new Dictionary<string, string>
        {
            { "Client-Id", _botSettings.ClientId }
        };

        var subscriptions = new List<(string, string)>
        {
            //{ "channel.subscribe", "channel.subscription.gift", "channel.subscription.message" };
            { ("channel.subscribe", "1") },
            { ("channel.subscription.gift", "1") },
            { ("channel.subscription.message", "1") },
            { ("channel.bits.use", "beta") },
            { ("channel.hype_train.begin", "1") },
            { ("channel.hype_train.progress", "1") },
            { ("channel.hype_train.end", "1") },
            { ("channel.follow", "2") },
            { ("channel.update", "2") },
            { ("stream.online", "1") },
            { ("stream.offline", "1") },
        };

        foreach (var subscription in subscriptions)
        {
            var message = new Message()
            {
                Payload = new()
                {
                    Subscription = new()
                    {
                        Type = subscription.Item1,
                        Version = subscription.Item2,
                        Condition = new()
                        {
                            BroadcasterUserId = channel,
                            ModeratorUserId = subscription.Item1.Equals("channel.follow") ? channel : null
                        },
                        Transport = new()
                        {
                            Method = "websocket",
                            SessionId = SessionId
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(message.Payload.Subscription, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var resp = await _httpService.PostAsync(_twitchSettings.EventSubscriptionUrl, content, MediaTypeNames.Application.Json, authHeader, headers, ct);

            var respContent = await resp?.Content?.ReadAsStringAsync();

            _logger.LogInformation("Response from subscription: {response}", respContent);
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

            if (result.MessageType == WebSocketMessageType.Close || Client.State == WebSocketState.CloseReceived)
            {
                _logger.LogWarning("Websocket connection closed. Reason: {reason}", result.CloseStatusDescription);
                await DisconnectAsync(ct);
                break;
            }

            //drop bytes that are 0 and only get the value back from the string itself
            var msg = Encoding.UTF8.GetString(buffer.Where(s => s != 0).ToArray());
            await HandleMessage(msg, callback, ct);
            Array.Clear(buffer, 0, buffer.Length);
        }
    }

    private async Task HandleMessage(string? message, Func<Message?, Task> callback, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (message.Contains("session_keepalive"))
            return;

        //_logger.LogDebug("Received message from twitch: {message}", message);
        Message? twitchMessage = null;
        try
        {
            twitchMessage = JsonConvert.DeserializeObject<Message>(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing message from twitch: {message}", message);
        }

        _logger.LogDebug("{channel} | {type} | {user} : {chatMessage}", twitchMessage?.Payload?.Event?.BroadcasterUserName, twitchMessage?.Payload?.Event?.MessageType, twitchMessage?.Payload?.Event?.ChatterUserName, twitchMessage?.Payload?.Event?.TwitchMessage?.Text);

        //need to intercept session id from welcome message
        var wsSessionId = twitchMessage?.Payload?.Session?.Id;
        if (!string.IsNullOrEmpty(wsSessionId) && !wsSessionId.Equals(SessionId))
            SessionId = wsSessionId;

        //clientwebsocket should handle the ping/pong messages
        await callback.Invoke(twitchMessage);
    }

    private async Task ReconnectAsync(Func<Message?, Task>? callback, CancellationToken ct = default)
    {
        await DisconnectAsync(ct);
        await ConnectAsync(callback, ct);
    }
}
