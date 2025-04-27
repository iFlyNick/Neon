using Confluent.Kafka;
using Neon.Core.Models.Kafka;
using Neon.Core.Services.Kafka;

namespace Neon.TwitchService.Consumers;

public class ChannelConsumer(ILogger<ChannelConsumer> logger, IKafkaService kafkaService) : BackgroundService
{
    private readonly ILogger<ChannelConsumer> _logger = logger;
    private readonly IKafkaService _kafkaService = kafkaService;

    private Dictionary<string, IList<string>?> _channels = [];

    private readonly string? _topic = "channel";
    private readonly string? _groupId = "channel-group";
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
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    private async Task OnConsumerMessageReceived(ConsumeResult<Ignore, string> result)
    {
        _logger.LogInformation("Received message: {message}", result.Message.Value);
    }

    private async Task OnConsumerException(ConsumeException e)
    {
        _logger.LogError("Error consuming message: {error}. Invoking callback.", e.Error.Reason);
    }
}
