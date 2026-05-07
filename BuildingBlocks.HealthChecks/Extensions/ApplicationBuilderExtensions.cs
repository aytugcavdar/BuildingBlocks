using System.Text.Json;
using BuildingBlocks.HealthChecks.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CoreHealthCheckOptions = BuildingBlocks.HealthChecks.Core.HealthCheckOptions;
using AspNetHealthCheckOptions = Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions;

namespace BuildingBlocks.HealthChecks.Extensions;

/// <summary>
/// Extension methods for configuring BuildingBlocks health checks in the application pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures BuildingBlocks health check endpoints in the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for fluent chaining.</returns>
    public static IApplicationBuilder UseBuildingBlocksHealthChecks(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<CoreHealthCheckOptions>() ?? new CoreHealthCheckOptions();

        // Liveness endpoint - checks if the application is alive
        app.UseHealthChecks(options.LivenessEndpoint, new AspNetHealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Liveness),
            ResponseWriter = WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Readiness endpoint - checks if the application is ready to receive traffic
        app.UseHealthChecks(options.ReadinessEndpoint, new AspNetHealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Readiness),
            ResponseWriter = WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Startup endpoint - checks if the application has completed startup
        app.UseHealthChecks(options.StartupEndpoint, new AspNetHealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Startup),
            ResponseWriter = WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // UI endpoint - optional health check dashboard
        if (options.EnableUI)
        {
            app.UseHealthChecksUI(setup =>
            {
                setup.UIPath = options.UIEndpoint;
            });
        }

        return app;
    }

    /// <summary>
    /// Writes the health check response as JSON.
    /// </summary>
    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data,
                tags = entry.Value.Tags
            })
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
