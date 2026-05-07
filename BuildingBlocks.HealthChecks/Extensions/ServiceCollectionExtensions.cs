using BuildingBlocks.HealthChecks.Core;
using BuildingBlocks.HealthChecks.Publishers;
using BuildingBlocks.HealthChecks.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.HealthChecks.Extensions;

/// <summary>
/// Extension methods for registering BuildingBlocks health checks in the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds BuildingBlocks health checks infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">The configuration section name. Default: "HealthChecks".</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddBuildingBlocksHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "HealthChecks")
    {
        // Bind and validate configuration
        var options = configuration.GetSection(sectionName).Get<HealthCheckOptions>() ?? new HealthCheckOptions();
        ValidateConfiguration(options);

        // Register options as singleton
        services.Configure<HealthCheckOptions>(configuration.GetSection(sectionName));
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckOptions>>().Value);

        // Register metrics
        services.AddSingleton<HealthCheckMetrics>();

        // Register health checks
        var builder = services.AddHealthChecks();

        // Configure caching via publisher options
        if (options.EnableCaching)
        {
            services.Configure<HealthCheckPublisherOptions>(publisherOptions =>
            {
                publisherOptions.Delay = TimeSpan.FromSeconds(options.DefaultCacheIntervalSeconds);
                publisherOptions.Period = TimeSpan.FromSeconds(options.DefaultCacheIntervalSeconds);
            });
        }

        // Register logging publisher if enabled
        if (options.EnablePublishers)
        {
            services.AddSingleton<IHealthCheckPublisher, LoggingHealthCheckPublisher>();
        }

        return builder;
    }

    private static void ValidateConfiguration(HealthCheckOptions options)
    {
        if (options.DefaultTimeoutSeconds < 1 || options.DefaultTimeoutSeconds > 60)
        {
            throw new InvalidOperationException(
                $"DefaultTimeoutSeconds must be between 1 and 60 seconds. Current value: {options.DefaultTimeoutSeconds}");
        }

        if (options.DefaultCacheIntervalSeconds < 1 || options.DefaultCacheIntervalSeconds > 300)
        {
            throw new InvalidOperationException(
                $"DefaultCacheIntervalSeconds must be between 1 and 300 seconds. Current value: {options.DefaultCacheIntervalSeconds}");
        }

        if (string.IsNullOrWhiteSpace(options.LivenessEndpoint))
        {
            throw new InvalidOperationException("LivenessEndpoint cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(options.ReadinessEndpoint))
        {
            throw new InvalidOperationException("ReadinessEndpoint cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(options.StartupEndpoint))
        {
            throw new InvalidOperationException("StartupEndpoint cannot be empty");
        }

        if (options.EnableUI && string.IsNullOrWhiteSpace(options.UIEndpoint))
        {
            throw new InvalidOperationException("UIEndpoint cannot be empty when EnableUI is true");
        }
    }
}
