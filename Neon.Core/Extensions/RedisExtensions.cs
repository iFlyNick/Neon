using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neon.Core.Services.Redis;

namespace Neon.Core.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration hostContext)
    {
        services.AddStackExchangeRedisCache(o =>
        {
            o.Configuration = hostContext.GetConnectionString("Redis");
            o.InstanceName = "Neon";
        });

        services.AddSingleton<IRedisService, RedisService>();

        return services;
    }
}
