namespace BuildingBlocks.HealthChecks.Core;

/// <summary>
/// Standard tags for categorizing health checks.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>
    /// Tag for liveness probe health checks.
    /// Liveness checks determine if a container should be restarted.
    /// </summary>
    public const string Liveness = "liveness";

    /// <summary>
    /// Tag for readiness probe health checks.
    /// Readiness checks determine if a container can receive traffic.
    /// </summary>
    public const string Readiness = "readiness";

    /// <summary>
    /// Tag for startup probe health checks.
    /// Startup checks execute once during application initialization.
    /// </summary>
    public const string Startup = "startup";

    /// <summary>
    /// Tag for database health checks.
    /// </summary>
    public const string Database = "database";

    /// <summary>
    /// Tag for cache health checks.
    /// </summary>
    public const string Cache = "cache";

    /// <summary>
    /// Tag for message broker health checks.
    /// </summary>
    public const string MessageBroker = "messagebroker";

    /// <summary>
    /// Tag for HTTP endpoint health checks.
    /// </summary>
    public const string Http = "http";

    /// <summary>
    /// Tag for system resource health checks (memory, disk).
    /// </summary>
    public const string System = "system";

    /// <summary>
    /// Tag for critical health checks.
    /// Critical checks returning Unhealthy result in overall Unhealthy status.
    /// </summary>
    public const string Critical = "critical";

    /// <summary>
    /// Tag for non-critical health checks.
    /// Non-critical checks returning Unhealthy result in overall Degraded status.
    /// </summary>
    public const string NonCritical = "noncritical";
}
