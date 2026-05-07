using BuildingBlocks.HealthChecks.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.HealthChecks.Extensions;

/// <summary>
/// Extension methods for registering HTTP endpoint health checks.
/// </summary>
public static class HttpHealthCheckExtensions
{
    /// <summary>
    /// Adds an HTTP endpoint health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="uri">The URI to check.</param>
    /// <param name="name">The health check name. If null, uses the URI host.</param>
    /// <param name="httpMethod">The HTTP method to use. Default: GET.</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddHttpEndpointHealthCheck(
        this IHealthChecksBuilder builder,
        Uri uri,
        string? name = null,
        HttpMethod? httpMethod = null,
        TimeSpan? timeout = null,
        params string[] tags)
    {
        var allTags = new List<string>
        {
            HealthCheckTags.Http,
            HealthCheckTags.Readiness,
            HealthCheckTags.NonCritical
        };
        allTags.AddRange(tags);

        var checkName = name ?? $"http-{uri.Host}";
        var method = httpMethod ?? HttpMethod.Get;

        return builder.AddUrlGroup(
            uri,
            name: checkName,
            failureStatus: HealthStatus.Unhealthy,
            tags: allTags,
            timeout: timeout);
    }

    /// <summary>
    /// Adds an HTTP endpoint health check using a string URI.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="uriString">The URI string to check.</param>
    /// <param name="name">The health check name. If null, uses the URI host.</param>
    /// <param name="httpMethod">The HTTP method to use. Default: GET.</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddHttpEndpointHealthCheck(
        this IHealthChecksBuilder builder,
        string uriString,
        string? name = null,
        HttpMethod? httpMethod = null,
        TimeSpan? timeout = null,
        params string[] tags)
    {
        return builder.AddHttpEndpointHealthCheck(
            new Uri(uriString),
            name,
            httpMethod,
            timeout,
            tags);
    }
}
