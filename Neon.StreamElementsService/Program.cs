using Coravel;
using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.StreamElementsService.Models;
using Neon.StreamElementsService.Models.Kafka;
using Neon.StreamElementsService.Services;
using Neon.StreamElementsService.Services.HealthChecks;
using Neon.StreamElementsService.Services.WebSocketManagers;
using Neon.StreamElementsService.Services.WebSockets;
using Neon.StreamElementsService.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddScheduler();
        
        services.ConfigureSerilog(hostContext.Configuration);
        services.ConfigureNeonDbContext(hostContext.Configuration);
        services.ConfigureTwitchServices(hostContext.Configuration);
        
        services.Configure<StreamElementsConfig>(hostContext.Configuration.GetSection("StreamElementsConfig"));
        services.Configure<NeonSettings>(hostContext.Configuration.GetSection("NeonSettings"));
        services.Configure<BaseKafkaConfig>(hostContext.Configuration.GetSection("BaseKafkaConfig"));

        //use named clients instead to avoid swapping auth headers around
        services.AddHttpClient();
        services.AddTransient<IHttpService, HttpService>();

        services.AddTransient<IKafkaService, KafkaService>();
        
        services.AddSingleton<IWebSocketManager, WebSocketManager>();
        services.AddTransient<IWebSocketService, WebSocketService>();
        
        services.AddSingleton<IHealthCheckService, WebSocketHealthCheck>();
        services.AddSingleton<HealthCheckWorker>();
        
        services.AddHostedService<StartupService>();
    })
    .Build();

host.Services.UseScheduler(s =>
{
    s.ScheduleAsync(async () =>
    {
        var worker = host.Services.GetRequiredService<HealthCheckWorker>();
        await worker.InvokeAsync();
    }).EveryMinute().PreventOverlapping(nameof(Program)).RunOnceAtStart();
});

await host.RunAsync();