using StackExchange.Redis;
using System.Text.Json;

namespace Fuel.Reporting.Service.Infrastructure.Cache;

public class RedisCacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value.ToString()) : null;
        }
        catch (RedisConnectionException)
        {
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiry);
        }
        catch (RedisConnectionException)
        {
            // Cache is optional; requests should continue when Redis is temporarily unavailable.
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (RedisConnectionException)
        {
            // Cache is optional; requests should continue when Redis is temporarily unavailable.
        }
    }
}
