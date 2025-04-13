using Neon.Core.Extensions;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.TwitchMessageService.Consumers;
using Neon.TwitchMessageService.Services.Twitch;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.ConfigureRedis(hostContext.Configuration);
        services.ConfigureSerilog(hostContext.Configuration);

        services.AddHttpClient<IHttpService, HttpService>();

        services.AddSingleton<IKafkaService, KafkaService>();
        services.AddScoped<ITwitchMessageService, TwitchMessageService>();

        services.AddHostedService<TwitchMessageConsumer>();
    })
    .Build();

await host.RunAsync();