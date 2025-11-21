using System.Net.WebSockets;
using Neon.StreamElementsService.Events;
using Neon.StreamElementsService.Models;

namespace Neon.StreamElementsService.Services.WebSockets;

public interface IWebSocketService
{
    bool IsConnected();
    public WebSocketState? GetWebSocketState();
    
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task SubscribeEventAsync(SubscriptionRequest? request, CancellationToken ct = default);
    
    event EventHandler<NotificationEventArgs>? OnNotificationEvent;
    event EventHandler<WebsocketClosedEventArgs>? OnWebsocketClosedEvent;
}