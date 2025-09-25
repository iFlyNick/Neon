using Confluent.Kafka;

namespace Neon.Core.Services.Kafka;

public interface IKafkaService
{
    void SubscribeConsumerEvent(ConsumerConfig? config, string? topic,
        Func<ConsumeResult<Ignore, string>, Task>? callback, Func<ConsumeException, Task>? exceptionCallback = null,
        CancellationToken ct = default);

    Task ProduceAsync(ProducerConfig? config, string? topic, string? key, string? message,
        Func<ProduceException<Null, string>, Task>? exceptionCallback = null, CancellationToken ct = default);
}