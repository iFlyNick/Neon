using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Neon.Persistence.Worker.Scripts;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.ConfigureSerilog(hostContext.Configuration);
        services.ConfigureNeonDbContext(hostContext.Configuration);
        services.ConfigureTwitchServices(hostContext.Configuration);
        
        services.AddHttpClient();
        services.AddTransient<IHttpService, HttpService>();
        
        services.Configure<TwitchSettings>(hostContext.Configuration.GetSection("TwitchSettings"));
        services.Configure<AppAccountSettings>(hostContext.Configuration.GetSection("AppAccountSettings"));
        services.Configure<NeonSettings>(hostContext.Configuration.GetSection("NeonSettings"));
        
        services.AddScoped<IDataGenerator, DataGenerator>();
    })
    .Build();

var scope = host.Services.CreateScope();

var dataGenerator = scope.ServiceProvider.GetRequiredService<IDataGenerator>();
await dataGenerator.PreloadDbData(CancellationToken.None);

await host.RunAsync();