namespace BuildingBlocks.CrossCutting.Caching.Configuration;

/// <summary>
/// Configuration settings for distributed caching.
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets the default TTL in seconds (0 = infinite).
    /// </summary>
    public int DefaultTtlSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Gets or sets the memory cache size limit in MB.
    /// </summary>
    public int MemoryCacheSizeLimitMb { get; set; } = 100;

    /// <summary>
    /// Gets or sets the compression threshold in bytes.
    /// </summary>
    public int CompressionThresholdBytes { get; set; } = 1024; // 1 KB

    /// <summary>
    /// Gets or sets the operation timeout in seconds.
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the warmup concurrency limit.
    /// </summary>
    public int WarmupConcurrencyLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether verbose logging is enabled.
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether L1 (memory) cache is enabled.
    /// </summary>
    public bool EnableL1Cache { get; set; } = true;

    /// <summary>
    /// Gets or sets whether L2 (Redis) cache is enabled.
    /// </summary>
    public bool EnableL2Cache { get; set; } = true;
}
