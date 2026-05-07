namespace BuildingBlocks.HealthChecks.Core;

/// <summary>
/// Configuration options for BuildingBlocks health checks.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Gets or sets the liveness endpoint path.
    /// Default: /health/live
    /// </summary>
    public string LivenessEndpoint { get; set; } = "/health/live";

    /// <summary>
    /// Gets or sets the readiness endpoint path.
    /// Default: /health/ready
    /// </summary>
    public string ReadinessEndpoint { get; set; } = "/health/ready";

    /// <summary>
    /// Gets or sets the startup endpoint path.
    /// Default: /health/startup
    /// </summary>
    public string StartupEndpoint { get; set; } = "/health/startup";

    /// <summary>
    /// Gets or sets the UI endpoint path.
    /// Default: /health/ui
    /// </summary>
    public string UIEndpoint { get; set; } = "/health/ui";

    /// <summary>
    /// Gets or sets the default timeout for health checks in seconds.
    /// Default: 5 seconds
    /// Valid range: 1-60 seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the default cache interval for health check results in seconds.
    /// Default: 30 seconds
    /// Valid range: 1-300 seconds
    /// </summary>
    public int DefaultCacheIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether the health check UI is enabled.
    /// Default: true
    /// </summary>
    public bool EnableUI { get; set; } = true;

    /// <summary>
    /// Gets or sets whether health check result caching is enabled.
    /// Default: true
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets whether health check publishers are enabled.
    /// Default: true
    /// </summary>
    public bool EnablePublishers { get; set; } = true;

    /// <summary>
    /// Gets or sets memory threshold configuration.
    /// </summary>
    public MemoryThresholds Memory { get; set; } = new();

    /// <summary>
    /// Gets or sets disk space threshold configuration.
    /// </summary>
    public DiskThresholds Disk { get; set; } = new();
}

/// <summary>
/// Memory threshold configuration for memory health checks.
/// </summary>
public class MemoryThresholds
{
    /// <summary>
    /// Gets or sets the degraded threshold in bytes.
    /// Default: 1 GB (1,073,741,824 bytes)
    /// </summary>
    public long DegradedThresholdBytes { get; set; } = 1_073_741_824;

    /// <summary>
    /// Gets or sets the unhealthy threshold in bytes.
    /// Default: 512 MB (536,870,912 bytes)
    /// </summary>
    public long UnhealthyThresholdBytes { get; set; } = 536_870_912;
}

/// <summary>
/// Disk space threshold configuration for disk space health checks.
/// </summary>
public class DiskThresholds
{
    /// <summary>
    /// Gets or sets the degraded threshold in bytes.
    /// Default: 10 GB (10,737,418,240 bytes)
    /// </summary>
    public long DegradedThresholdBytes { get; set; } = 10_737_418_240;

    /// <summary>
    /// Gets or sets the unhealthy threshold in bytes.
    /// Default: 5 GB (5,368,709,120 bytes)
    /// </summary>
    public long UnhealthyThresholdBytes { get; set; } = 5_368_709_120;

    /// <summary>
    /// Gets or sets the path to monitor for disk space.
    /// Default: / (root)
    /// </summary>
    public string MonitoredPath { get; set; } = "/";
}
