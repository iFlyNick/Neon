﻿namespace Neon.TwitchService.Services.WebSocketManagers;

public interface IWebSocketManager
{
    Task Subscribe(string? broadcasterName, CancellationToken ct = default);
    Task Unsubscribe(string? broadcasterName, CancellationToken ct = default);
    Task SubscribeBotToChat(string? botName, string? broadcasterName, string? overrideBroadcasterId = null, CancellationToken ct = default);
}
