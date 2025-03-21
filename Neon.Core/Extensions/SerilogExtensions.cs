using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Neon.Core.Extensions;

public static class SerilogExtensions
{
    public static IServiceCollection ConfigureSerilog(this IServiceCollection services, IConfiguration hostContext)
    {
        services.AddLogging(s =>
        {
            s.ClearProviders()
                .AddSerilog(new LoggerConfiguration()
                    .ReadFrom.Configuration(hostContext)
                    .CreateLogger());
        });

        return services;
    }
}
