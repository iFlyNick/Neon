using Confluent.Kafka;
using Neon.Core.Services.Kafka;

namespace Neon.TwitchService.Consumers;

public class ChannelConsumer(ILogger<ChannelConsumer> logger, IKafkaService kafkaService) : BackgroundService
{
    private Dictionary<string, IList<string>?> _channels = [];

    private const string Topic = "channel";
    private const string GroupId = "channel-group";

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        InitializeChannelConsumer(ct);

        return Task.CompletedTask;
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
            BootstrapServers = "localhost:9092",
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    private async Task OnConsumerMessageReceived(ConsumeResult<Ignore, string> result)
    {
        logger.LogInformation("Received message: {message}", result.Message.Value);
        await Task.CompletedTask;
    }

    private async Task OnConsumerException(ConsumeException e)
    {
        logger.LogError("Error consuming message: {error}.", e.Error.Reason);
        await Task.CompletedTask;
    }
}
