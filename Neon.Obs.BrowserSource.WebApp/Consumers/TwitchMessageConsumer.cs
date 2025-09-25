using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Kafka;
using Neon.Core.Services.Kafka;
using Neon.Obs.BrowserSource.WebApp.Hubs;
using Neon.Obs.BrowserSource.WebApp.Models;
using Newtonsoft.Json;

namespace Neon.Obs.BrowserSource.WebApp.Consumers;

public class TwitchMessageConsumer(ILogger<TwitchMessageConsumer> logger, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IHubContext<ChatHub> chatHub, IOptions<BaseKafkaConfig> kafkaConfig) : BackgroundService
{
    private readonly BaseKafkaConfig _kafkaConfig = kafkaConfig.Value ?? throw new ArgumentNullException(nameof(kafkaConfig));
    
    private readonly string? _topic = "twitch-channel-processed-messages";
    private readonly string? _groupId = "twitch-channel-processed-messages-group";
    private readonly string? _partitionKey = "0";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        InitializeChannelConsumer(ct);

        await Task.CompletedTask;
    }

    private void InitializeChannelConsumer(CancellationToken ct = default)
    {
        var config = GetConsumerConfig();

        kafkaService.SubscribeConsumerEvent(config, OnConsumerMessageReceived, OnConsumerException, ct);
    }

    private KafkaConsumerConfig GetConsumerConfig()
    {
        return new KafkaConsumerConfig
        {
            Topic = _topic,
            TargetPartition = _partitionKey,
            BootstrapServers = _kafkaConfig.BootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    private async Task OnConsumerMessageReceived(ConsumeResult<Ignore, string> result)
    {
        try
        {
            var message = result.Message.Value;

            if (message is null)
                return;

            using var scope = serviceScopeFactory.CreateScope();

            var jsonMessage = JsonConvert.DeserializeObject<TwitchMessage>(message);

            if (jsonMessage is null || string.IsNullOrEmpty(jsonMessage.ChannelId))
            {
                logger.LogDebug("Received null or invalid message: {message}", message);
                return;
            }
            
            await chatHub.Clients.Group(jsonMessage.ChannelId).SendAsync("ReceiveMessage", jsonMessage);
        }
        catch (Exception ex)
        {
            logger.LogError("Error processing message: {error}", ex.Message);
        }
    }

    private async Task OnConsumerException(ConsumeException e)
    {
        logger.LogError("Error consuming message: {error}. Invoking callback.", e.Error.Reason);
    }
}