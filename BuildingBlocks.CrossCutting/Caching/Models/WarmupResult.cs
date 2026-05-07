namespace BuildingBlocks.CrossCutting.Caching.Models;

/// <summary>
/// Result of a cache warmup operation.
/// </summary>
public class WarmupResult
{
    /// <summary>
    /// Gets or sets the number of successfully warmed entries.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed entries.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the warmup operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the keys that failed to warm up.
    /// </summary>
    public List<string> FailedKeys { get; set; } = new();
}
