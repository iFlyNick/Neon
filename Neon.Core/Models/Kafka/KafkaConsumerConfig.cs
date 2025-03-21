using Confluent.Kafka;

namespace Neon.Core.Models.Kafka;

public class KafkaConsumerConfig : ConsumerConfig
{
    public string? Topic { get; set; }
    public string? TargetPartition { get; set; }
}
