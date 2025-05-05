using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Chatbot;
using Neon.Core.Models.Kafka;
using Neon.Core.Services.Kafka;
using Neon.TwitchChatbotService.Models;
using Neon.TwitchChatbotService.Services.Messaging;
using Newtonsoft.Json;

namespace Neon.TwitchChatbotService.Consumers;

public class TwitchChatbotConsumer(ILogger<TwitchChatbotConsumer> logger, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IOptions<AppBaseConfig> appBaseConfig) : BackgroundService
{
    private readonly AppBaseConfig _appBaseConfig = appBaseConfig.Value ?? throw new ArgumentNullException(nameof(appBaseConfig));
    
    private readonly string? _topic = "twitch-chatbot-messages";
    private readonly string? _groupId = "twitch-chatbot-messages-group";
    private readonly string? _partitionKey = "0";
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        InitializeChannelConsumer(ct);
        await Task.CompletedTask;
    }
    
    private void InitializeChannelConsumer(CancellationToken ct = default)
    {
        var config = GetConsumerConfig();

        kafkaService.SubscribeConsumerEvent(config, OnConsumerMessageReceived, OnConsumerException, ct);
    }
    
    private KafkaConsumerConfig GetConsumerConfig()
    {
        return new KafkaConsumerConfig
        {
            Topic = _topic,
            TargetPartition = _partitionKey,
            BootstrapServers = _appBaseConfig.KafkaBootstrapServers,
            GroupId = _groupId,
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

            var twitchMessage = JsonConvert.DeserializeObject<ChatbotMessage>(message);

            var msgService = scope.ServiceProvider.GetRequiredService<IMessagingService>();
            
            await msgService.ProcessMessage(twitchMessage);
        }
        catch (Exception ex)
        {
            logger.LogError("Error processing message: {error}", ex.Message);
        }
    }
    
    private async Task OnConsumerException(ConsumeException e)
    {
        logger.LogError("Error consuming message: {error}. Invoking callback.", e.Error.Reason);
        await Task.CompletedTask;
    }
}