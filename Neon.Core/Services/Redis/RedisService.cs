using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Neon.Core.Services.Redis;

public class RedisService(ILogger<RedisService> logger, IDistributedCache cache) : IRedisService
{
    public async Task<bool> Exists(string? key, CancellationToken ct = default)
    {
        var value = await Get(key, ct);

        return !string.IsNullOrEmpty(value);
    }

    public async Task<string?> Get(string? key, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            logger.LogError("Key is null or empty.");
            return null;
        }

        var value = await cache.GetStringAsync(key, ct);

        if (string.IsNullOrEmpty(value))
        {
            logger.LogWarning("Key {key} not found in cache.", key);
            return null;
        }

        //_logger.LogDebug("Found key {key} in cache. Value: {value}", key, value);
        return value;
    }

    public async Task Create(string? key, string? value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            logger.LogError("Key or value is null or empty.");
            return;
        }

        var options = new DistributedCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }

        await cache.SetStringAsync(key, value, options, ct);
    }

    public async Task Remove(string? key, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            logger.LogDebug("Key is null or empty. Unable to remove from cache.");
            return;
        }
        
        var keyExists = await Exists(key, ct);
        if (!keyExists)
            return;
        
        await cache.RemoveAsync(key, ct);
    }

    //public async Task Update(string? key, string? value, TimeSpan? expiration = null, CancellationToken ct = default)
    //{

    //}

    //public async Task Delete(string? key, CancellationToken ct = default)
    //{

    //}
}
