using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Neon.Core.Services.Redis;

public class RedisService(ILogger<RedisService> logger, IDistributedCache cache) : IRedisService
{
    private readonly ILogger<RedisService> _logger = logger;
    private readonly IDistributedCache _cache = cache;

    public async Task<bool> Exists(string? key, CancellationToken ct = default)
    {
        var value = await Get(key, ct);

        return !string.IsNullOrEmpty(value);
    }

    public async Task<string?> Get(string? key, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogError("Key is null or empty.");
            return null;
        }

        var value = await _cache.GetStringAsync(key, ct);

        if (string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("Key {key} not found in cache.", key);
            return null;
        }

        return value;
    }

    public async Task Create(string? key, string? value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            _logger.LogError("Key or value is null or empty.");
            return;
        }

        var options = new DistributedCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }

        await _cache.SetStringAsync(key, value, options, ct);
    }

    //public async Task Update(string? key, string? value, TimeSpan? expiration = null, CancellationToken ct = default)
    //{

    //}

    //public async Task Delete(string? key, CancellationToken ct = default)
    //{

    //}
}
