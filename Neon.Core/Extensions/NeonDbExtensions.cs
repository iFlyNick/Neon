using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neon.Persistence.NeonContext;

namespace Neon.Core.Extensions;

public static class NeonDbExtensions
{
    public static IServiceCollection ConfigureNeonDbContext(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NeonDbContext>(options =>
        {
            options.UseNpgsql(config.GetConnectionString("NeonDb"));
        });
        return services;
    }
}
