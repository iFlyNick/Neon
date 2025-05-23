using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.Core.Services.Twitch.Helix;
using Neon.TwitchService.Models.Kafka;
using Neon.TwitchService.Services;
using Neon.TwitchService.Services.WebSocketManagers;
using Neon.TwitchService.Services.WebSockets;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.ConfigureSerilog(hostContext.Configuration);
        services.ConfigureNeonDbContext(hostContext.Configuration);
        services.ConfigureTwitchServices(hostContext.Configuration);

        services.Configure<TwitchSettings>(hostContext.Configuration.GetSection("TwitchSettings"));
        services.Configure<NeonSettings>(hostContext.Configuration.GetSection("NeonSettings"));
        services.Configure<BaseKafkaConfig>(hostContext.Configuration.GetSection("BaseKafkaConfig"));

        //use named clients instead to avoid swapping auth headers around
        services.AddHttpClient();
        services.AddTransient<IHttpService, HttpService>();

        services.AddTransient<IKafkaService, KafkaService>();

        services.AddScoped<IStartupService, StartupService>();

        services.AddSingleton<IWebSocketManager, WebSocketManager>();
        services.AddTransient<IWebSocketService, WebSocketService>();
    })
    .Build();

var scope = host.Services.CreateScope();

var startupService = scope.ServiceProvider.GetRequiredService<IStartupService>();
await startupService.SubscribeAllActiveChannels(CancellationToken.None);

await host.RunAsync();