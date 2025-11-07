using Neon.Core.Models.Twitch;
using Neon.TwitchService.Events;
using Neon.TwitchService.Models;

namespace Neon.TwitchService.Services.WebSockets;

public interface IWebSocketService
{
    void SetNeonTwitchBotSettings(NeonTwitchBotSettings? botSettings);
    DateTimeOffset GetLastMessageReceived();
    bool IsConnected();
    bool? IsReconnectRequested();
    string? GetSessionId();
    void SetChatterId(string? chatterId);
    string? GetChatterId();
    void SetBroadcasterId(string? broadcasterId);
    string? GetBroadcasterId();
    Task ConnectAsync(string? wsUrl, CancellationToken ct = default);
    Task DisconnectAsync(bool sendClose, CancellationToken ct = default);
    Task SubscribeChannelAsync(string? broadcasterId, string? accessToken, List<SubscriptionType>? subscriptions, CancellationToken ct = default);
    Task SubscribeChannelChatAsync(string? broadcasterId, string? chatterId, string? accessToken, List<SubscriptionType>? subscriptions, CancellationToken ct = default);
 
    event EventHandler<SessionReconnectEventArgs>? OnReconnectEvent;
    event EventHandler<RevocationEventArgs>? OnRevocationEvent;
    event EventHandler<NotificationEventArgs>? OnNotificationEvent;
    event EventHandler<WebsocketClosedEventArgs>? OnWebsocketClosedEvent;
    event EventHandler<KeepAliveFailureEventArgs>? OnKeepAliveFailureEvent;
}
