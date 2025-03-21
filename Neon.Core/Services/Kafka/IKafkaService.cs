using Confluent.Kafka;
using Neon.Core.Models.Kafka;

namespace Neon.Core.Services.Kafka;

public interface IKafkaService
{
    void SubscribeConsumerEvent(KafkaConsumerConfig? config, Func<ConsumeResult<Ignore, string>, Task>? callback, Func<ConsumeException, Task>? exceptionCallback = null, CancellationToken ct = default);
    Task ProduceAsync(KafkaProducerConfig? config, string? message, Func<ProduceException<Null, string>, Task>? exceptionCallback = null, CancellationToken ct = default);
}