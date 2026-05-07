using BuildingBlocks.HealthChecks.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.HealthChecks.Extensions;

/// <summary>
/// Extension methods for registering Redis health checks.
/// </summary>
public static class RedisHealthCheckExtensions
{
    /// <summary>
    /// Adds a Redis health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="name">The health check name. Default: "redis".</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddRedisHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "redis",
        TimeSpan? timeout = null,
        params string[] tags)
    {
        var allTags = new List<string>
        {
            HealthCheckTags.Cache,
            HealthCheckTags.Readiness,
            HealthCheckTags.NonCritical
        };
        allTags.AddRange(tags);

        return builder.AddRedis(
            connectionString,
            name: name,
            failureStatus: HealthStatus.Unhealthy,
            tags: allTags,
            timeout: timeout);
    }
}
