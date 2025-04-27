using Neon.Core.Extensions;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.TwitchMessageService.Consumers;
using Neon.TwitchMessageService.Models;
using Neon.TwitchMessageService.Services.Twitch;
using Neon.TwitchMessageService.Services.Twitch.Badges;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.ConfigureRedis(hostContext.Configuration);
        services.ConfigureSerilog(hostContext.Configuration);

        services.Configure<AppBaseConfig>(hostContext.Configuration.GetSection("AppBaseConfig"));
        
        services.AddHttpClient();
        services.AddTransient<IHttpService, HttpService>();

        services.AddTransient<IKafkaService, KafkaService>();
        services.AddScoped<ITwitchMessageService, TwitchMessageService>();
        services.AddScoped<IBadgeService, BadgeService>();

        services.AddHostedService<TwitchMessageConsumer>();
    })
    .Build();

await host.RunAsync();