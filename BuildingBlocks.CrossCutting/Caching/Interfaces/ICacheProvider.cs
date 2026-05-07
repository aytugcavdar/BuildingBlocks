namespace BuildingBlocks.CrossCutting.Caching.Interfaces;

/// <summary>
/// Cache provider interface for abstracting different cache implementations (Memory, Redis).
/// </summary>
public interface ICacheProvider
{
    /// <summary>
    /// Gets the provider name (e.g., "Memory", "Redis").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache with optional expiration.
    /// </summary>
    Task SetAsync(string key, byte[] value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans for keys matching a pattern.
    /// </summary>
    Task<IEnumerable<string>> ScanKeysAsync(string pattern, int maxKeys = 10000, CancellationToken cancellationToken = default);
}
