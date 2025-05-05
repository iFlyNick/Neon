using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.TwitchChatbotService.Consumers;
using Neon.TwitchChatbotService.Models;
using Neon.TwitchChatbotService.Services.Events;
using Neon.TwitchChatbotService.Services.Messaging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.ConfigureNeonDbContext(hostContext.Configuration);
        services.ConfigureSerilog(hostContext.Configuration);
        services.ConfigureTwitchServices(hostContext.Configuration);

        services.AddHttpClient();
        services.AddTransient<IHttpService, HttpService>();
        
        services.Configure<NeonSettings>(hostContext.Configuration.GetSection("NeonSettings"));
        services.Configure<AppBaseConfig>(hostContext.Configuration.GetSection("AppBaseConfig"));
        services.Configure<TwitchSettings>(hostContext.Configuration.GetSection("TwitchSettings"));

        services.AddTransient<IKafkaService, KafkaService>();
        
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IMessagingService, MessagingService>();
        
        services.AddHostedService<TwitchEventConsumer>();
        services.AddHostedService<TwitchChatbotConsumer>();
    })
    .Build();

await host.RunAsync();