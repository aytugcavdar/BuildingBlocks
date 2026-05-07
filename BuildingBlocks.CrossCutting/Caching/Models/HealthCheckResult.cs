namespace BuildingBlocks.CrossCutting.Caching.Models;

/// <summary>
/// Result of a cache health check.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Gets or sets whether the cache is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the error message if unhealthy.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
