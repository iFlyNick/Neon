using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Kafka;
using Neon.Core.Models.Twitch.EventSub;
using Neon.Core.Services.Kafka;
using Neon.TwitchChatbotService.Models;
using Neon.TwitchChatbotService.Services.Events;
using Newtonsoft.Json;

namespace Neon.TwitchChatbotService.Consumers;

public class TwitchEventConsumer(
    ILogger<TwitchEventConsumer> logger,
    IServiceScopeFactory serviceScopeFactory,
    IKafkaService kafkaService,
    IOptions<AppBaseConfig> appBaseConfig) : BackgroundService
{
    private readonly AppBaseConfig _appBaseConfig =
        appBaseConfig.Value ?? throw new ArgumentNullException(nameof(appBaseConfig));

    private readonly string? _topic = "twitch-channel-events";
    private readonly string? _groupId = "twitch-channel-events-group";
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
            BootstrapServers = _appBaseConfig.KafkaBootstrapServers,
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

            var twitchMessage = JsonConvert.DeserializeObject<Message>(message);
            
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var chatbotMessage = eventService.ProcessMessage(twitchMessage);

            await kafkaService.ProduceAsync(new KafkaProducerConfig
            {
                Topic = "twitch-chatbot-messages",
                TargetPartition = "0",
                BootstrapServers = _appBaseConfig.KafkaBootstrapServers
            }, JsonConvert.SerializeObject(chatbotMessage));
        }
        catch (Exception ex)
        {
            logger.LogError("Error processing message: {error}", ex.Message);
        }
    }

    private async Task OnConsumerException(ConsumeException e)
    {
        logger.LogError("Error consuming message: {error}. Invoking callback.", e.Error.Reason);
        await Task.CompletedTask;
    }
}
