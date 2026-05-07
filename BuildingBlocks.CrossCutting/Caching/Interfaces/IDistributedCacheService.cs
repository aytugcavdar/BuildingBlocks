using BuildingBlocks.CrossCutting.Caching.Models;

namespace BuildingBlocks.CrossCutting.Caching.Interfaces;

/// <summary>
/// Distributed cache service interface providing multi-level caching with Redis and in-memory providers.
/// </summary>
public interface IDistributedCacheService
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache with optional expiration and tags.
    /// </summary>
    Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached values matching a pattern.
    /// </summary>
    Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached values associated with a tag.
    /// </summary>
    Task<int> RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics including hit rate and operation durations.
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Warms up the cache with a collection of key-value pairs.
    /// </summary>
    Task<WarmupResult> WarmupAsync(IEnumerable<KeyValuePair<string, object>> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health of the cache providers.
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
