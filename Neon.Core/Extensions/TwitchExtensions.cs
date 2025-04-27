using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neon.Core.Data.Twitch;
using Neon.Core.Services.Twitch.Authentication;
using Neon.Core.Services.Twitch.Helix;

namespace Neon.Core.Extensions;

public static class TwitchExtensions
{
    public static IServiceCollection ConfigureTwitchServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITwitchDbService, TwitchDbService>();
        services.AddScoped<IHelixService, HelixService>();
        services.AddScoped<IAppTokenService, AppTokenService>();
        services.AddScoped<IUserTokenService, UserTokenService>();
        services.AddSingleton<IOAuthService, OAuthService>();

        return services;
    }
}
