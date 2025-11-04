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
    void SetChatUser(string? chatUser);
    string? GetChatUser();
    void SetChannel(string? channel);
    string? GetChannel();
    Task ConnectAsync(string? wsUrl, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task SubscribeChannelAsync(string? channel, string? accessToken, List<SubscriptionType>? subscriptions, CancellationToken ct = default);
    Task SubscribeChannelChatAsync(string? twitchChannelId, string? userId, string? accessToken, List<SubscriptionType>? subscriptions, CancellationToken ct = default);
 
    event EventHandler<SessionReconnectEventArgs>? OnReconnectEvent;
    event EventHandler<RevocationEventArgs>? OnRevocationEvent;
    event EventHandler<NotificationEventArgs>? OnNotificationEvent;
    event EventHandler<WebsocketClosedEventArgs>? OnWebsocketClosedEvent;
    event EventHandler<KeepAliveFailureEventArgs>? OnKeepAliveFailureEvent;
}
