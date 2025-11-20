using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Neon.StreamElementsService.Events;
using Neon.StreamElementsService.Models;

namespace Neon.StreamElementsService.Services.WebSockets;

public class WebSocketService(ILogger<WebSocketService> logger, IOptions<StreamElementsConfig> seConfig) : IWebSocketService
{
    private readonly StreamElementsConfig _seConfig = seConfig.Value;
    
    private ClientWebSocket? _client;
    
    private bool WsConnected => (_client?.State ?? WebSocketState.None) == WebSocketState.Open;
    private const int RetryCount = 5;
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(10);
    
    public event EventHandler<NotificationEventArgs>? OnNotificationEvent;
    public event EventHandler<WebsocketClosedEventArgs>? OnWebsocketClosedEvent;
    
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await TryConnectAsync(ct);
    }

    private async Task TryConnectAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_seConfig.WsUrl))
        {
            logger.LogError("StreamElements WebSocket URL is null or empty.");
            return;
        }

        if (_client is not null && WsConnected)
        {
            logger.LogDebug("StreamElements WebSocket Client is already connected.");
            return;
        }
        
        _client = new ClientWebSocket();
        logger.LogInformation("Connecting websocket at {time} to {wsUrl} | Hash: {hash}", DateTime.UtcNow, _seConfig.WsUrl, GetHashCode());

        var attempts = 1;
        var connected = false;
        while (!ct.IsCancellationRequested && attempts <= RetryCount && !connected)
        {
            try
            {
                _client = new ClientWebSocket();
                await _client.ConnectAsync(new Uri(_seConfig.WsUrl), ct);

                if (_client.State == WebSocketState.Open)
                {
                    connected = true;
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("WebSocket connection attempt was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error connecting to StreamElements WebSocket on attempt {attempt} of {maxAttempts} | Message: {message} | Hash: {hash}", attempts, RetryCount, ex.Message, GetHashCode());

                if (attempts >= RetryCount)
                {
                    logger.LogError("Max connection attempts reached. Unable to connect to StreamElements WebSocket.");
                    throw;
                }
                
                logger.LogInformation("Waiting {delay} before next connection attempt...", _retryDelay);
                await Task.Delay(_retryDelay, ct);
            }
            
            _client.Dispose();
            attempts++;
        }
        
        logger.LogInformation("Connected to StreamElements WebSocket at {time}. Attempts: {attempts} | Hash: {hash}", DateTime.UtcNow, attempts, GetHashCode());

        _ = Task.Run(async () => await ListenAsync(ct), ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_client is null || !WsConnected)
        {
            logger.LogInformation("StreamElements WebSocket Client is already disconnected.");
            return;
        }
        
        try
        {
            logger.LogInformation("Disconnecting from StreamElements WebSocket...");
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", ct);
            logger.LogInformation("Disconnected from StreamElements WebSocket.");
            
            _client.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disconnecting from StreamElements WebSocket: {message}", ex.Message);
        }
    }

    private async Task ListenAsync(CancellationToken ct = default)
    {
        var buffer = new byte[1024 * 8];
        var segment = new ArraySegment<byte>(buffer);

        while (WsConnected && !ct.IsCancellationRequested)
        {
            var result = await _client!.ReceiveAsync(segment, ct);

            if (result.MessageType == WebSocketMessageType.Close || _client.State == WebSocketState.CloseReceived ||
                _client.State == WebSocketState.CloseSent || _client.State == WebSocketState.Closed ||
                _client.State == WebSocketState.Aborted)
            {
                logger.LogDebug("Websocket connection closed. Reason: {reason} | Hash: {hash}", result.CloseStatusDescription, GetHashCode());
                OnWebsocketClosed(result.CloseStatusDescription);
                break;
            }
            
            var msg = Encoding.UTF8.GetString(buffer.Where(s => s != 0).ToArray());
            HandleMessage(msg);
            Array.Clear(buffer, 0, buffer.Length);
        }
        
        logger.LogDebug("Websocket listen async method has excited the while loop, the connection has been closed. Hash: {hash}", GetHashCode());
    }

    private void HandleMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        
        logger.LogDebug(message);

        Message? seMessage = null;
        try
        {
            seMessage = JsonConvert.DeserializeObject<Message>(message,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deserializing StreamElements WebSocket message: {message}", ex.Message);
        }
        
        if (seMessage is null)
        {
            logger.LogDebug("Deserialized StreamElements message is null!");
            return;
        }
        
        if (seMessage.Type == "message")
            OnNotificationReceived(seMessage);
        
        logger.LogDebug("StreamElements WebSocket message type received: {messageType}", seMessage.Type);
    }
    
    private void OnWebsocketClosed(string? reason)
    {
        var args = new WebsocketClosedEventArgs
        {
            Reason = reason,
            EventDate = DateTime.UtcNow
        };

        var handler = OnWebsocketClosedEvent;
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
    
    public async Task SubscribeEventAsync(SubscriptionRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            logger.LogDebug("StreamElements WebSocket subscribe request is null. Nothing defined to subscribe to!");
            return;
        }
        
        var jsonRequest = JsonConvert.SerializeObject(request);
        var bytes = Encoding.UTF8.GetBytes(jsonRequest);
        await _client!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }
}