using BuildingBlocks.HealthChecks.Checks;
using BuildingBlocks.HealthChecks.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.HealthChecks.Extensions;

/// <summary>
/// Extension methods for registering system resource health checks.
/// </summary>
public static class SystemHealthCheckExtensions
{
    /// <summary>
    /// Adds a memory health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Default: "memory".</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddMemoryHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "memory",
        TimeSpan? timeout = null,
        params string[] tags)
    {
        var allTags = new List<string>
        {
            HealthCheckTags.System,
            HealthCheckTags.Readiness,
            HealthCheckTags.NonCritical
        };
        allTags.AddRange(tags);

        return builder.AddCheck<MemoryHealthCheck>(
            name: name,
            failureStatus: HealthStatus.Unhealthy,
            tags: allTags,
            timeout: timeout);
    }

    /// <summary>
    /// Adds a disk space health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Default: "disk".</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddDiskSpaceHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "disk",
        TimeSpan? timeout = null,
        params string[] tags)
    {
        var allTags = new List<string>
        {
            HealthCheckTags.System,
            HealthCheckTags.Readiness,
            HealthCheckTags.NonCritical
        };
        allTags.AddRange(tags);

        return builder.AddCheck<DiskSpaceHealthCheck>(
            name: name,
            failureStatus: HealthStatus.Unhealthy,
            tags: allTags,
            timeout: timeout);
    }
}
