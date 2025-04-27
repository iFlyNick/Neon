using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Neon.Core.Models.Kafka;

namespace Neon.Core.Services.Kafka;

public class KafkaService(ILogger<KafkaService> logger) : IKafkaService
{
    private readonly ILogger<KafkaService> _logger = logger;

    private IProducer<string, string>? _producer;

    public void SubscribeConsumerEvent(KafkaConsumerConfig? config, Func<ConsumeResult<Ignore, string>, Task>? callback, Func<ConsumeException, Task>? exceptionCallback = null, CancellationToken ct = default)
    {
        ConsumerConfigValidationChecks(config);
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        //consumers aren't async by default, so spin up new thread to run consumer and raise events back out
        var t = Task.Run(() =>
        {
            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            var partition = GetPartitionByKey(config!.BootstrapServers, config.Topic, config.TargetPartition);

            if (partition == -1)
            {
                _logger.LogError("Failed to get partition for key {key}", config.TargetPartition);
                return;
            }

            consumer.Assign(new TopicPartitionOffset(config.Topic, new Partition(partition), Offset.End));

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(ct);
                    callback.Invoke(result);
                }
                catch (ConsumeException e)
                {
                    _logger.LogError("Error consuming message: {error}. Invoking callback.", e.Error.Reason);
                    exceptionCallback?.Invoke(e);
                }
            }

        }, ct);
    }

    public async Task ProduceAsync(KafkaProducerConfig? config, string? message, Func<ProduceException<Null, string>, Task>? exceptionCallback = null, CancellationToken ct = default)
    {
        if (_producer is null)
        {
            ProducerConfigValidationChecks(config);
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        if (string.IsNullOrEmpty(message))
        {
            _logger.LogWarning("No message passed to producer method. Skipping producer call");
            return;
        }

        try
        {
            var msg = new Message<string, string> { Key = config!.TargetPartition!, Value = message };

            var deliveryReport = await _producer.ProduceAsync(config!.Topic, msg, ct);
            //_logger.LogDebug("Delivered message to {topic} with target partition {partition}", config.Topic, config.TargetPartition);
        }
        catch (ProduceException<Null, string> e)
        {
            _logger.LogError("Delivery failed: {error}. Invoking callback.", e.Error.Reason);
            exceptionCallback?.Invoke(e);
        }
    }

    private static void ConsumerConfigValidationChecks(KafkaConsumerConfig? config)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        ArgumentNullException.ThrowIfNull(config.BootstrapServers, nameof(config.BootstrapServers));
        ArgumentNullException.ThrowIfNull(config.GroupId, nameof(config.GroupId));
        ArgumentNullException.ThrowIfNull(config.Topic, nameof(config.Topic));
        ArgumentNullException.ThrowIfNull(config.TargetPartition, nameof(config.TargetPartition));
    }

    private static void ProducerConfigValidationChecks(KafkaProducerConfig? config)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        ArgumentNullException.ThrowIfNull(config.BootstrapServers, nameof(config.BootstrapServers));
        ArgumentNullException.ThrowIfNull(config.Topic, nameof(config.Topic));
        ArgumentNullException.ThrowIfNull(config.TargetPartition, nameof(config.TargetPartition));
    }

    private int GetPartitionByKey(string? bootstrapServer, string? topic, string? partitionKey)
    {
        if (string.IsNullOrEmpty(bootstrapServer) || string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(partitionKey))
            return -1;

        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServer }).Build();
        var metadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(10));
        if (metadata.Topics.Count > 0)
        {
            var topicMetadata = metadata.Topics.First();
            var partition = topicMetadata.Partitions.FirstOrDefault(p => p.PartitionId == partitionKey.GetHashCode() % topicMetadata.Partitions.Count);
            return partition is null ? -1 : partition.PartitionId;
        }
        return -1;
    }
}
