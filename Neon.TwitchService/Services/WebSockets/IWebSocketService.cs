using Neon.Core.Models.Twitch;
using Neon.TwitchService.Models;
using Neon.Core.Models.Twitch.EventSub;

namespace Neon.TwitchService.Services.WebSockets;

public interface IWebSocketService
{
    void SetNeonTwitchBotSettings(NeonTwitchBotSettings? botSettings);
    bool IsConnected();
    Task ConnectAsync(Func<Message?, Task> callback, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task SendMessageAsync(string? message, CancellationToken ct = default);
    Task SubscribeChannelAsync(string? channel, string? accessToken, List<SubscriptionType>? subscriptions, CancellationToken ct = default);
    Task SubscribeChannelChatAsync(string? twitchChannelId, string? userId, string? accessToken, List<SubscriptionType>? subscriptions, CancellationToken ct = default);
    Task UnsubscribeChannelAsync(string? channel, List<SubscriptionType>? subscriptions, CancellationToken ct = default);
}
