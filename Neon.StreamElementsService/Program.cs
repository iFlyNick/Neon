using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.StreamElementsService.Models;
using Neon.StreamElementsService.Models.Kafka;
using Neon.StreamElementsService.Services;
using Neon.StreamElementsService.Services.WebSocketManagers;
using Neon.StreamElementsService.Services.WebSockets;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
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
        
        services.AddHostedService<StartupService>();
    })
    .Build();

await host.RunAsync();