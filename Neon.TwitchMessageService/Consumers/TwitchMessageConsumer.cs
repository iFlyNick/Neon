using Confluent.Kafka;
using Neon.Core.Models.Kafka;
using Neon.Core.Services.Kafka;
using Neon.TwitchMessageService.Services.Twitch;
using Newtonsoft.Json;

namespace Neon.TwitchMessageService.Consumers;

public class TwitchMessageConsumer(ILogger<TwitchMessageConsumer> logger, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService) : BackgroundService
{
    private readonly ILogger<TwitchMessageConsumer> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IKafkaService _kafkaService = kafkaService;

    private readonly string? _topic = "twitch-channel-chats";
    private readonly string? _groupId = "twitch-channel-messages-group";
    private readonly string? _partitionKey = "0";

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        InitializeChannelConsumer(ct);

        return Task.CompletedTask;
    }

    private void InitializeChannelConsumer(CancellationToken ct = default)
    {
        var config = GetConsumerConfig();

        _kafkaService.SubscribeConsumerEvent(config, OnConsumerMessageReceived, OnConsumerException, ct);
    }

    private KafkaConsumerConfig GetConsumerConfig()
    {
        return new KafkaConsumerConfig
        {
            Topic = _topic,
            TargetPartition = _partitionKey,
            BootstrapServers = "localhost:9092",
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }

    private async Task OnConsumerMessageReceived(ConsumeResult<Ignore, string> result)
    {
        var message = result.Message.Value;

        if (message is null)
            return;

        using var scope = _serviceScopeFactory.CreateScope();

        var msgService = scope.ServiceProvider.GetRequiredService<ITwitchMessageService>();

        var processedMessage = await msgService.ProcessTwitchMessage(message);

        await _kafkaService.ProduceAsync(new KafkaProducerConfig
        {
            Topic = "twitch-channel-processed-messages",
            TargetPartition = "0",
            BootstrapServers = "localhost:9092"
        }, JsonConvert.SerializeObject(processedMessage));
    }

    private async Task OnConsumerException(ConsumeException e)
    {
        _logger.LogError("Error consuming message: {error}. Invoking callback.", e.Error.Reason);
    }
}
