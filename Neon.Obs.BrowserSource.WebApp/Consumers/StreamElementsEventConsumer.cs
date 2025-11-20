using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Neon.Core.Services.Kafka;
using Neon.Obs.BrowserSource.WebApp.Hubs;
using Neon.Obs.BrowserSource.WebApp.Models;
using Neon.Obs.BrowserSource.WebApp.Models.StreamElements;
using Neon.Obs.BrowserSource.WebApp.Services.StreamElements;
using Newtonsoft.Json;

namespace Neon.Obs.BrowserSource.WebApp.Consumers;

public class StreamElementsEventConsumer(ILogger<StreamElementsEventConsumer> logger, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IHubContext<ChatHub> chatHub, IOptions<BaseKafkaConfig> kafkaConfig) : BackgroundService
{
    private readonly BaseKafkaConfig _kafkaConfig = kafkaConfig.Value ?? throw new ArgumentNullException(nameof(kafkaConfig));
    
    private readonly string? Topic = "streamelements-channel-events";
    private readonly string? GroupId = "streamelements-channel-events-group-obs-webapp";

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
            
            var eventService = scope.ServiceProvider.GetRequiredService<IStreamElementsEventService>();

            var jsonMessage = JsonConvert.DeserializeObject<Message>(message);
            
            if (jsonMessage is null || string.IsNullOrEmpty(jsonMessage.Room))
            {
                logger.LogDebug("Received null or invalid message: {message}", message);
                return;
            }
            
            var processedMessage = await eventService.ProcessMessage(jsonMessage);

            if (processedMessage is null || string.IsNullOrEmpty(processedMessage.ChannelId) ||
                string.IsNullOrEmpty(processedMessage.EventMessage))
            {
                logger.LogDebug("Skipping sending null channel or message to obs frontend for streamelements event. ChannelId: {channelId}, EventMessage: {eventMessage}", processedMessage?.ChannelId, processedMessage?.EventMessage);
            }
            
            logger.LogDebug("Sending streamelements event to obs frontend for ChannelId: {channelId}, EventType: {eventType}", processedMessage!.ChannelId, processedMessage.EventType);
            await chatHub.Clients.Group(processedMessage!.ChannelId!).SendAsync("ReceiveStreamElementsEvent", processedMessage);
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