using BuildingBlocks.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.HealthChecks.Extensions;

/// <summary>
/// Extension methods for registering database health checks.
/// </summary>
public static class DatabaseHealthCheckExtensions
{
    /// <summary>
    /// Adds a PostgreSQL health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="name">The health check name. Default: "postgresql".</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddPostgreSqlHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "postgresql",
        TimeSpan? timeout = null,
        params string[] tags)
    {
        var allTags = new List<string>
        {
            HealthCheckTags.Database,
            HealthCheckTags.Readiness,
            HealthCheckTags.Critical
        };
        allTags.AddRange(tags);

        return builder.AddNpgSql(
            connectionString,
            name: name,
            failureStatus: HealthStatus.Unhealthy,
            tags: allTags,
            timeout: timeout);
    }

    /// <summary>
    /// Adds a SQL Server health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="name">The health check name. Default: "sqlserver".</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddSqlServerHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "sqlserver",
        TimeSpan? timeout = null,
        params string[] tags)
    {
        var allTags = new List<string>
        {
            HealthCheckTags.Database,
            HealthCheckTags.Readiness,
            HealthCheckTags.Critical
        };
        allTags.AddRange(tags);

        return builder.AddSqlServer(
            connectionString,
            name: name,
            failureStatus: HealthStatus.Unhealthy,
            tags: allTags,
            timeout: timeout);
    }

    /// <summary>
    /// Adds a DbContext health check.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. If null, uses the DbContext type name.</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddDbContextHealthCheck<TContext>(
        this IHealthChecksBuilder builder,
        string? name = null,
        TimeSpan? timeout = null,
        params string[] tags)
        where TContext : DbContext
    {
        var allTags = new List<string>
        {
            HealthCheckTags.Database,
            HealthCheckTags.Readiness,
            HealthCheckTags.Critical
        };
        allTags.AddRange(tags);

        return builder.AddCheck<DbContextHealthCheck<TContext>>(
            name: name ?? typeof(TContext).Name,
            failureStatus: HealthStatus.Unhealthy,
            tags: allTags,
            timeout: timeout);
    }
}

/// <summary>
/// Health check for Entity Framework DbContext connectivity.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public class DbContextHealthCheck<TContext> : IHealthCheck where TContext : DbContext
{
    private readonly TContext _context;

    public DbContextHealthCheck(TContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                return HealthCheckResult.Healthy($"DbContext {typeof(TContext).Name} is healthy");
            }

            return HealthCheckResult.Unhealthy($"DbContext {typeof(TContext).Name} cannot connect");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"DbContext {typeof(TContext).Name} check failed",
                exception: ex);
        }
    }
}
