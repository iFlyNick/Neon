using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Neon.Core.Services.Kafka;
using Neon.TwitchService.Models;
using Neon.TwitchService.Models.Kafka;
using Neon.TwitchService.Services.WebSocketManagers;

namespace Neon.TwitchService.Services.HealthChecks;

public class WebSocketHealthCheck(ILogger<WebSocketHealthCheck> logger, IWebSocketManager webSocketManager, IKafkaService kafkaService, IOptions<BaseKafkaConfig> baseKafkaSettings) : IHealthCheck, IHealthCheckService
{
    private const string KafkaTopic = "neon-health-checks";
    private readonly BaseKafkaConfig _baseKafkaSettings = baseKafkaSettings.Value ?? throw new ArgumentNullException(nameof(baseKafkaSettings));

    private ProducerConfig? _config;
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext? context, CancellationToken ct = default)
    {
        var wsServices = webSocketManager.GetWebSocketServices().ToList();

        string? msg;
        
        if (wsServices.Count == 0)
        {
            logger.LogInformation("No websocket services found.");
            msg = "No websocket services found, returning healthy.";
            await SendKafkaHealthCheckResult(msg, ct);
            return await Task.FromResult(HealthCheckResult.Healthy(msg));
        }
        
        var unhealthyServices = wsServices.Where(ws => !ws.IsConnected()).ToList();

        if (unhealthyServices.Count == 0)
        {
            logger.LogInformation("All websocket services are healthy. Total services: {serviceCount}", wsServices.Count);
            msg = $"All websocket services are healthy. Total services: {wsServices.Count}";
            await SendKafkaHealthCheckResult(msg, ct);
            return await Task.FromResult(HealthCheckResult.Healthy(msg));
        }
        
        //unhealth services found, set status to degraded
        logger.LogWarning("{unhealthyCount} unhealthy websocket services found.", unhealthyServices.Count);
        msg = $"{unhealthyServices.Count} unhealthy websocket services found.";
        await SendKafkaHealthCheckResult(msg, ct);
        return await Task.FromResult(HealthCheckResult.Degraded(msg));
    }

    private async Task SendKafkaHealthCheckResult(string description, CancellationToken ct = default)
    {
        _config ??= new ProducerConfig { BootstrapServers = _baseKafkaSettings.BootstrapServers };

        try
        {
            var healthCheckMessage = new HealthCheckMessage
            {
                ServiceName = nameof(WebSocketHealthCheck),
                Description = description,
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