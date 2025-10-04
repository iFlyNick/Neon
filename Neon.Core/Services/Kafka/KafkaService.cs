using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Neon.Core.Services.Kafka;

public class KafkaService(ILogger<KafkaService> logger) : IKafkaService
{
    private IProducer<string, string>? _producer;

    public void SubscribeConsumerEvent(ConsumerConfig? config, string? topic, Func<ConsumeResult<Ignore, string>, Task>? callback, Func<ConsumeException, Task>? exceptionCallback = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        //consumers aren't async by default, so spin up new thread to run consumer and raise events back out
        try
        {
            Task.Run(async () =>
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

                consumer.Subscribe(topic);

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(ct);
                        await callback.Invoke(result);
                    }
                    catch (ConsumeException e)
                    {
                        logger.LogError("Error consuming message: {error}. Invoking callback.", e.Error.Reason);
                        exceptionCallback?.Invoke(e);
                    }
                }

            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Kafka consumer. Topic: {topic}", topic);
            throw;
        }
    }

    public async Task ProduceAsync(ProducerConfig? config, string? topic, string? key, string? message, Func<ProduceException<Null, string>, Task>? exceptionCallback = null, CancellationToken ct = default)
    {
        if (config?.BootstrapServers is null)
        {
            logger.LogError("Invalid producer config passed to ProduceAsync method or config bootstrap servers is undefined. Aborting produce call.");
            return;
        }
        
        _producer ??= new ProducerBuilder<string, string>(config).Build();

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(topic))
        {
            logger.LogWarning("Missing key, message, or topic passed to producer method. Skipping producer call. Key: {key} | Message: {message} | Topic: {topic}", string.IsNullOrEmpty(key) ? "no value passed" : "value passed", string.IsNullOrEmpty(message) ? "no value passed" : "value passed", string.IsNullOrEmpty(topic) ? "no value passed" : "value passed");
            return;
        }

        try
        {
            var msg = new Message<string, string> { Key = key, Value = message };

            var deliveryReport = await _producer.ProduceAsync(topic, msg, ct);

            if (deliveryReport.Status != PersistenceStatus.Persisted)
                logger.LogError("Message delivery failed for key {key} to topic {topic}.", key, topic);
        }
        catch (ProduceException<Null, string> e)
        {
            logger.LogError("Delivery failed: {error}. Invoking callback.", e.Error.Reason);
            exceptionCallback?.Invoke(e);
        }
    }
}
