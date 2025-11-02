using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Neon.Core.Services.Kafka;
using Neon.Core.Services.Twitch.Authentication;
using Neon.TwitchService.Models;
using Neon.TwitchService.Models.Kafka;
using Neon.TwitchService.Services.WebSocketManagers;

namespace Neon.TwitchService.Services.HealthChecks;

public class WebSocketHealthCheck(ILogger<WebSocketHealthCheck> logger, IServiceScopeFactory serviceScopeFactory, IWebSocketManager webSocketManager, IKafkaService kafkaService, IOptions<BaseKafkaConfig> baseKafkaSettings) : IHealthCheck, IHealthCheckService
{
    private const string KafkaTopic = "neon-health-checks";
    private readonly BaseKafkaConfig _baseKafkaSettings = baseKafkaSettings.Value ?? throw new ArgumentNullException(nameof(baseKafkaSettings));

    private ProducerConfig? _config;
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext? context, CancellationToken ct = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var userTokenService = scope.ServiceProvider.GetRequiredService<IUserTokenService>();
        
        var wsServices = webSocketManager.GetWebSocketServices().ToList();

        var wsStatuses = new List<WebSocketHealthDetail>();
        
        foreach (var ws in wsServices)
        {
            var sessionId = ws.GetSessionId();
            var channel = ws.GetChannel();
            var chatUser = ws.GetChatUser();
            var isConnected = ws.IsConnected();

            var coreUser = string.IsNullOrEmpty(chatUser) ? channel : chatUser;
            var userToken = await userTokenService.GetUserAuthTokenByBroadcasterId(coreUser, ct);

            var subscriptions = await webSocketManager.GetSubscriptions(userToken, sessionId, chatUser, channel, ct);

            var convertedSubList = subscriptions?.Select(s => new WebSocketSubscriptionDetail
            {
                Id = s.Id,
                Status = s.Status,
                Type = s.Type,
                CreatedAt = s.CreatedAt,
                ConnectedAt = s.Transport?.ConnectedAt,
                DisconnectedAt = s.DisconnectedAt
            }).ToList();
            
            wsStatuses.Add(new WebSocketHealthDetail
            {
                SessionId = sessionId,
                Channel = channel,
                ChatUser = chatUser,
                IsConnected = isConnected,
                Subscriptions = convertedSubList
            });
        }
        
        var wsStatusesJson = JsonSerializer.Serialize(wsStatuses);
        
        string? msg;
        HealthCheckResult healthCheckResult;
        
        if (wsServices.Count == 0)
        {
            logger.LogInformation("No websocket services found.");
            msg = "No websocket services found, returning healthy.";
            healthCheckResult = HealthCheckResult.Healthy(msg);
            await SendKafkaHealthCheckResult(msg, healthCheckResult, ct);
            return await Task.FromResult(healthCheckResult);
        }
        
        var unhealthyServices = wsServices.Where(ws => !ws.IsConnected()).ToList();

        if (unhealthyServices.Count == 0)
        {
            logger.LogInformation("All websocket services are healthy. Total services: {serviceCount}", wsServices.Count);
            msg = $"All websocket services are healthy. Total services: {wsServices.Count} | Statuses: {wsStatusesJson}";
            healthCheckResult = HealthCheckResult.Healthy(msg);
            await SendKafkaHealthCheckResult(msg, healthCheckResult, ct);
            return await Task.FromResult(healthCheckResult);
        }
        
        //unhealth services found, set status to degraded
        logger.LogWarning("{unhealthyCount} unhealthy websocket services found out of expected {wsCount} services.", unhealthyServices.Count, wsServices.Count);
        msg = $"{unhealthyServices.Count} unhealthy websocket services found. Total services: {wsServices.Count} | Statuses: {wsStatusesJson}";
        healthCheckResult = unhealthyServices.Count == wsServices.Count ? HealthCheckResult.Unhealthy(msg) : HealthCheckResult.Degraded(msg);
        await SendKafkaHealthCheckResult(msg, healthCheckResult, ct);
        return await Task.FromResult(healthCheckResult);
    }

    private async Task SendKafkaHealthCheckResult(string description, HealthCheckResult overallResult, CancellationToken ct = default)
    {
        _config ??= new ProducerConfig { BootstrapServers = _baseKafkaSettings.BootstrapServers };

        try
        {
            var healthCheckMessage = new HealthCheckMessage
            {
                ServiceName = nameof(WebSocketHealthCheck),
                Description = description,
                OverallStatus = overallResult.Status,
                Timestamp = DateTime.UtcNow
            };

            var jsonMsg = JsonSerializer.Serialize(healthCheckMessage);
            
            await kafkaService.ProduceAsync(_config, KafkaTopic, nameof(WebSocketHealthCheck), jsonMsg, null, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send health check message to Kafka: {message}", ex.Message);
        }
    }
}