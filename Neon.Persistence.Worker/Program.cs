using Neon.Core.Extensions;
using Neon.Persistence.Scripts;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.ConfigureSerilog(hostContext.Configuration);
        services.ConfigureNeonDbContext(hostContext.Configuration);
        
        services.AddScoped<IDataGenerator, DataGenerator>();
    })
    .Build();

var scope = host.Services.CreateScope();

var dataGenerator = scope.ServiceProvider.GetRequiredService<IDataGenerator>();
await dataGenerator.PreloadDbData(CancellationToken.None);

await host.RunAsync();