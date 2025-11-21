using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Neon.Core.Services.Kafka;
using Neon.StreamElementsService.Events;
using Neon.StreamElementsService.Models;
using Neon.StreamElementsService.Models.Kafka;
using Neon.StreamElementsService.Services.WebSockets;
using Newtonsoft.Json;

namespace Neon.StreamElementsService.Services.WebSocketManagers;

public class WebSocketManager(ILogger<WebSocketManager> logger, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IOptions<BaseKafkaConfig> baseKafkaSettings) : IWebSocketManager
{
    private readonly BaseKafkaConfig _baseKafkaConfig = baseKafkaSettings.Value ?? throw new ArgumentNullException(nameof(baseKafkaSettings));
    
    private const string ProducerTopicEvents = "streamelements-channel-events";

    //map of broadcaster id to se room id
    private readonly Dictionary<string, string> _channels = new();
    
    public IList<IWebSocketService> GetWebSocketServices() => _webSocketServices.AsReadOnly();
    private readonly List<IWebSocketService> _webSocketServices = [];
    
    private void OnNotificationEvent(object? sender, NotificationEventArgs e) => _ = HandleNotificationEvent(sender, e);
    private void OnWebsocketClosedEvent(object? sender, WebsocketClosedEventArgs e) => _ = HandleWebsocketClosedEvent(sender, e);
    
    private async Task HandleNotificationEvent(object? sender, NotificationEventArgs e)
    {
        try
        {
            var seMessage = e.Message;
            if (seMessage is null)
            {
                logger.LogDebug("OnNotificationEvent: StreamElements message is null!");
                return;
            }
            
            var seChannel = seMessage.Data?.Channel;
            if (string.IsNullOrEmpty(seChannel))
            {
                logger.LogDebug("OnNotificationEvent: StreamElements channel is null!");
                return;
            }
            
            //get broadcaster id from dictionary
            var broadcasterId = _channels[seChannel];
            logger.LogDebug("Matched notification event from stream elements to broadcaster id: {broadcasterId}", broadcasterId);
            
            await kafkaService.ProduceAsync(new ProducerConfig
                {
                    BootstrapServers = _baseKafkaConfig.BootstrapServers
                },
                ProducerTopicEvents,
                broadcasterId,
                JsonConvert.SerializeObject(seMessage),
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
        logger.LogDebug("StreamElements Websocket closed event raised. Reason: {reason}", e.Reason);
        _webSocketServices.Clear();
    }

    public async Task Subscribe(string? broadcasterId, string? channelId, string? jwtToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(jwtToken))
        {
            logger.LogDebug("StreamElements WebSocketManager Subscribe: Missing broadcasterId, channelId or jwtToken!");
            return;
        }
        
        using var scope = serviceScopeFactory.CreateScope();
        var webSocketService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
        
        webSocketService.OnNotificationEvent += OnNotificationEvent;
        webSocketService.OnWebsocketClosedEvent += OnWebsocketClosedEvent;
        
        if (!_webSocketServices.Contains(webSocketService))
            _webSocketServices.Add(webSocketService);
        
        _channels[channelId] = broadcasterId;
        
        await webSocketService.ConnectAsync(ct);

        var seRequest = new SubscriptionRequest
        {
            Type = "subscribe",
            Nonce = Guid.NewGuid().ToString(),
            Data = new SubscriptionRequestData
            {
                Topic = "channel.tips.moderation",
                Room = channelId,
                Token = jwtToken,
                TokenType = "jwt"
            }
        };
        
        await webSocketService.SubscribeEventAsync(seRequest, ct);
    }
}