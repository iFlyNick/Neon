using Confluent.Kafka;
using Microsoft.Extensions.Options;
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

    private const string? Topic = "twitch-channel-events";
    private const string? GroupId = "twitch-channel-events-group";

    private const string? ProducerTopic = "twitch-chatbot-messages";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        InitializeChannelConsumer(ct);
        await Task.CompletedTask;
    }

    private void InitializeChannelConsumer(CancellationToken ct = default)
    {
        var config = GetConsumerConfig();

        kafkaService.SubscribeConsumerEvent(config, Topic, OnConsumerMessageReceived, OnConsumerException, ct);
    }

    private ConsumerConfig GetConsumerConfig()
    {
        return new ConsumerConfig
        {
            BootstrapServers = _appBaseConfig.KafkaBootstrapServers,
            GroupId = GroupId,
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

            await kafkaService.ProduceAsync(new ProducerConfig
                {
                    BootstrapServers = _appBaseConfig.KafkaBootstrapServers
                },
                ProducerTopic,
                chatbotMessage?.ChannelId,
                JsonConvert.SerializeObject(chatbotMessage)
            );
        }
        catch (Exception ex)
        {
            logger.LogError("Error processing message: {error}", ex.Message);
        }
    }

    private async Task OnConsumerException(ConsumeException e)
    {
        logger.LogError("Error consuming message: {error}.", e.Error.Reason);
        await Task.CompletedTask;
    }
}
