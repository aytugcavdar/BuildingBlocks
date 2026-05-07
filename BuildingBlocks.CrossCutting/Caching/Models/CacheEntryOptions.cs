namespace BuildingBlocks.CrossCutting.Caching.Models;

/// <summary>
/// Options for cache entry expiration and tagging.
/// </summary>
public class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets the absolute expiration time.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration duration.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with this cache entry.
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets the effective expiration time span.
    /// </summary>
    public TimeSpan? GetExpiration()
    {
        if (AbsoluteExpiration.HasValue)
        {
            return AbsoluteExpiration.Value - DateTimeOffset.UtcNow;
        }

        return SlidingExpiration;
    }
}
