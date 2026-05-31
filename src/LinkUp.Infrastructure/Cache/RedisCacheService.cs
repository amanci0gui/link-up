using System.Text.Json;
using LinkUp.Application.Common.Interfaces;
using StackExchange.Redis;

namespace LinkUp.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
        => _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue) return default;
        return JsonSerializer.Deserialize<T>(value!.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);

    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
        => await _db.StringSetAsync(key, value, expiry, When.NotExists);
}
