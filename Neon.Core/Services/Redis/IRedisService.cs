namespace Neon.Core.Services.Redis;

public interface IRedisService
{
    Task<bool> Exists(string? key, CancellationToken ct = default);
    Task<string?> Get(string? key, CancellationToken ct = default);
    Task Create(string? key, string? value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task Remove(string? key, CancellationToken ct = default);
    //Task Update(string? key, string? value, TimeSpan? expiration = null, CancellationToken ct = default);
}
