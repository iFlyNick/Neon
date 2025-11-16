using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Twitch.EventSub;
using Neon.Core.Services.Kafka;
using Neon.Obs.BrowserSource.WebApp.Hubs;
using Neon.Obs.BrowserSource.WebApp.Models;
using Neon.Obs.BrowserSource.WebApp.Services.Events;
using Newtonsoft.Json;

namespace Neon.Obs.BrowserSource.WebApp.Consumers;

public class TwitchEventConsumer(ILogger<TwitchEventConsumer> logger, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IHubContext<ChatHub> chatHub, IOptions<BaseKafkaConfig> kafkaConfig) : BackgroundService
{
    private readonly BaseKafkaConfig _kafkaConfig = kafkaConfig.Value ?? throw new ArgumentNullException(nameof(kafkaConfig));
    
    private readonly string? Topic = "twitch-channel-events";
    private readonly string? GroupId = "twitch-channel-events-group-obs-webapp";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        InitializeChannelConsumer(ct);

        await Task.CompletedTask;
    }

    private void InitializeChannelConsumer(CancellationToken ct = default)
    {
        var config = GetConsumerConfig();
        logger.LogDebug("Attempting to subscribe to Kafka topic: {topic} with group ID: {groupId}", Topic, GroupId);
        kafkaService.SubscribeConsumerEvent(config, Topic, OnConsumerMessageReceived, OnConsumerException, ct);
    }

    private ConsumerConfig GetConsumerConfig()
    {
        return new ConsumerConfig
        {
            BootstrapServers = _kafkaConfig.BootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }
    
    private async Task OnConsumerMessageReceived(ConsumeResult<Ignore, string> result)
    {
        try
        {
            logger.LogDebug("Received event from Kafka topic: {topic} | Partition: {partition} | Offset: {offset}", result.Topic, result.Partition, result.Offset);
            var message = result.Message.Value;

            if (message is null)
                return;

            using var scope = serviceScopeFactory.CreateScope();
            
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var jsonMessage = JsonConvert.DeserializeObject<Message>(message);
            
            if (jsonMessage is null || string.IsNullOrEmpty(jsonMessage.Payload?.Event?.BroadcasterUserId))
            {
                logger.LogDebug("Received null or invalid message: {message}", message);
                return;
            }
            
            var processedMessage = eventService.ProcessMessage(jsonMessage);

            if (string.IsNullOrEmpty(jsonMessage.Payload?.Event?.BroadcasterUserId))
                return;
            
            await chatHub.Clients.Group(jsonMessage.Payload?.Event?.BroadcasterUserId!).SendAsync("ReceiveEvent", processedMessage);
        }
        catch (Exception ex)
        {
            logger.LogError("Error processing event: {error}", ex.Message);
        }
    }

    private async Task OnConsumerException(ConsumeException e)
    {
        logger.LogError("Error consuming event: {error}. Invoking callback.", e.Error.Reason);
        await Task.CompletedTask;
    }
}