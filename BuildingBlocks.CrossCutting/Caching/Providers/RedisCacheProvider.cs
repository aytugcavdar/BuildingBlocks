using BuildingBlocks.CrossCutting.Caching.Exceptions;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using StackExchange.Redis;

namespace BuildingBlocks.CrossCutting.Caching.Providers;

/// <summary>
/// Redis cache provider using StackExchange.Redis.
/// </summary>
public class RedisCacheProvider : ICacheProvider
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public string ProviderName => "Redis";

    public RedisCacheProvider(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _database = _redis.GetDatabase();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            return value.HasValue ? (byte[]?)value : null;
        }
        catch (Exception ex)
        {
            throw new CacheProviderException(ProviderName, $"Failed to get key '{key}'", ex);
        }
    }

    public async Task SetAsync(string key, byte[] value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.StringSetAsync(key, value, expiration);
        }
        catch (Exception ex)
        {
            throw new CacheProviderException(ProviderName, $"Failed to set key '{key}'", ex);
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            throw new CacheProviderException(ProviderName, $"Failed to remove key '{key}'", ex);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            throw new CacheProviderException(ProviderName, $"Failed to check existence of key '{key}'", ex);
        }
    }

    public async Task<IEnumerable<string>> ScanKeysAsync(string pattern, int maxKeys = 10000, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = new List<string>();

            await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 1000))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                keys.Add(key.ToString());

                if (keys.Count >= maxKeys)
                    break;
            }

            return keys;
        }
        catch (Exception ex)
        {
            throw new CacheProviderException(ProviderName, $"Failed to scan keys with pattern '{pattern}'", ex);
        }
    }
}
