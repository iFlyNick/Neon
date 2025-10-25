using Coravel.Invocable;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neon.TwitchService.Services.HealthChecks;

namespace Neon.TwitchService.Workers;

public class HealthCheckWorker(ILogger<HealthCheckWorker> logger, IEnumerable<IHealthCheckService> hcServices) : ICancellableInvocable
{
    private bool _firstCheck = true;
    private const int FirstDelay = 30000;

    public CancellationToken CancellationToken { get; set; }
    
    public async Task InvokeAsync()
    {
        CancellationToken.ThrowIfCancellationRequested();

        //bypassing the first health check to allow services to start
        if (_firstCheck)
        {
            logger.LogInformation("Bypassing first health check for {Delay}ms to allow services to start.", FirstDelay);
            _firstCheck = false;
            await Task.Delay(FirstDelay, CancellationToken);
            return;
        }

        var rList = new List<HealthStatus>();

        if (!hcServices.Any())
        {
            logger.LogInformation("No health check services registered!.");
            return;
        }
        
        logger.LogDebug("Running health checks for {ServiceCount} services.", hcServices.Count());
        
        foreach (var hcService in hcServices)
        {
            var hc = await hcService.CheckHealthAsync(new HealthCheckContext(), CancellationToken);
            rList.Add(hc.Status);
        }

        var result =
            rList.All(s => s.Equals(HealthStatus.Healthy))
                ? HealthStatus.Healthy
                : rList.All(s => s.Equals(HealthStatus.Unhealthy))
                    ? HealthStatus.Unhealthy
                    : HealthStatus.Degraded;
        
        logger.LogInformation("Overall Health check result: {HealthStatus}", result);
    }
}