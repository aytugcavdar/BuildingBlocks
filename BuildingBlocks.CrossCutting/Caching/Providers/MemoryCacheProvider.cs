using System.Collections.Concurrent;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingBlocks.CrossCutting.Caching.Providers;

/// <summary>
/// In-memory cache provider using IMemoryCache with LRU eviction.
/// </summary>
public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keyTracker = new();

    public string ProviderName => "Memory";

    public MemoryCacheProvider(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = _cache.Get<byte[]>(key);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, byte[] value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            Size = value.Length
        };

        if (expiration.HasValue && expiration.Value > TimeSpan.Zero)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }

        // Register eviction callback to remove from key tracker
        options.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            _keyTracker.TryRemove(key.ToString()!, out _);
        });

        _cache.Set(key, value, options);
        _keyTracker.TryAdd(key, 0);

        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        var removed = _keyTracker.TryRemove(key, out _);
        return Task.FromResult(removed);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public Task<IEnumerable<string>> ScanKeysAsync(string pattern, int maxKeys = 10000, CancellationToken cancellationToken = default)
    {
        // Convert Redis-style pattern to regex
        var regexPattern = "^" + pattern
            .Replace("*", ".*")
            .Replace("?", ".")
            + "$";

        var regex = new System.Text.RegularExpressions.Regex(regexPattern);

        var matchingKeys = _keyTracker.Keys
            .Where(key => regex.IsMatch(key))
            .Take(maxKeys)
            .ToList();

        return Task.FromResult<IEnumerable<string>>(matchingKeys);
    }
}
