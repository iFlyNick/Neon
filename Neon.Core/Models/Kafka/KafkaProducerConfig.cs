using Confluent.Kafka;

namespace Neon.Core.Models.Kafka;

public class KafkaProducerConfig : ProducerConfig
{
    public string? Topic { get; set; }
    public string? TargetPartition { get; set; }
}
