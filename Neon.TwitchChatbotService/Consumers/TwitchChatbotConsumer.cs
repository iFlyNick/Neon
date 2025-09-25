using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Chatbot;
using Neon.Core.Services.Kafka;
using Neon.TwitchChatbotService.Models;
using Neon.TwitchChatbotService.Services.Messaging;
using Newtonsoft.Json;

namespace Neon.TwitchChatbotService.Consumers;

public class TwitchChatbotConsumer(ILogger<TwitchChatbotConsumer> logger, IServiceScopeFactory serviceScopeFactory, IKafkaService kafkaService, IOptions<AppBaseConfig> appBaseConfig) : BackgroundService
{
    private readonly AppBaseConfig _appBaseConfig = appBaseConfig.Value ?? throw new ArgumentNullException(nameof(appBaseConfig));
    
    private const string? Topic = "twitch-chatbot-messages";
    private const string? GroupId = "twitch-chatbot-messages-group";
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        InitializeChannelConsumer(ct);
        await Task.CompletedTask;
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
            BootstrapServers = _appBaseConfig.KafkaBootstrapServers,
            GroupId = GroupId,
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