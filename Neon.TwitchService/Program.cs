using Coravel;
using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.TwitchService.Models;
using Neon.TwitchService.Models.Kafka;
using Neon.TwitchService.Services;
using Neon.TwitchService.Services.HealthChecks;
using Neon.TwitchService.Services.OAuthValidations;
using Neon.TwitchService.Services.WebSocketManagers;
using Neon.TwitchService.Services.WebSockets;
using Neon.TwitchService.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddScheduler();
        
        services.ConfigureSerilog(hostContext.Configuration);
        services.ConfigureNeonDbContext(hostContext.Configuration);
        services.ConfigureTwitchServices(hostContext.Configuration);

        services.Configure<TwitchSettings>(hostContext.Configuration.GetSection("TwitchSettings"));
        services.Configure<NeonSettings>(hostContext.Configuration.GetSection("NeonSettings"));
        services.Configure<BaseKafkaConfig>(hostContext.Configuration.GetSection("BaseKafkaConfig"));
        services.Configure<NeonStartupSettings>(hostContext.Configuration.GetSection("NeonStartupSettings"));

        //use named clients instead to avoid swapping auth headers around
        services.AddHttpClient();
        services.AddTransient<IHttpService, HttpService>();

        services.AddTransient<IKafkaService, KafkaService>();
        
        services.AddSingleton<IWebSocketManager, WebSocketManager>();
        services.AddTransient<IWebSocketService, WebSocketService>();

        services.AddHealthChecks().AddCheck<WebSocketHealthCheck>("WebSocketHealthCheck");
        
        services.AddSingleton<IHealthCheckService, WebSocketHealthCheck>();
        services.AddSingleton<HealthCheckWorker>();

        services.AddScoped<IOAuthValidationService, OAuthValidationService>();
        services.AddSingleton<OAuthValidationWorker>();
        
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

    s.ScheduleAsync(async () =>
    {
        var worker = host.Services.GetRequiredService<OAuthValidationWorker>();
        await worker.InvokeAsync();
    }).Hourly().PreventOverlapping("OAuthValidationWorker").RunOnceAtStart();
});

await host.RunAsync();