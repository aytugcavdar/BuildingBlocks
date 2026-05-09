using BuildingBlocks.CrossCutting.Authentication;
using BuildingBlocks.CrossCutting.Caching.Extensions;
using BuildingBlocks.CrossCutting.Exceptions.Extensions;
using BuildingBlocks.CrossCutting.Locking;
using BuildingBlocks.CrossCutting.RateLimiting;
using BuildingBlocks.HealthChecks.Extensions;
using BuildingBlocks.Logging.Extensions;
using BuildingBlocks.Messaging;
using BuildingBlocks.Security;
using Microsoft.AspNetCore.Builder;

namespace BuildingBlocks.Composition;

public static class BuildingBlocksDefaultsExtensions
{
    public static WebApplicationBuilder AddBuildingBlocksDefaults(
        this WebApplicationBuilder builder,
        Action<BuildingBlocksDefaultsOptions>? configure = null)
    {
        var options = CreateOptions(builder, configure);
        var services = builder.Services;
        var configuration = builder.Configuration;

        if (options.UseLogging)
        {
            builder.AddBuildingBlocksLogging(options.ApplicationName!);
        }

        if (options.UseExceptionHandling)
        {
            services.AddBuildingBlocksExceptionHandling();
        }

        if (options.UseJwtAuthentication)
        {
            services.AddBuildingBlocksJwtAuthentication(configuration);
        }

        if (options.UseSecurityServices)
        {
            services.AddBuildingBlocksSecurity(security =>
            {
                security.SkipJwtRegistration = options.UseJwtAuthentication;
            });
        }

        if (options.UseCaching)
        {
            services.AddBuildingBlocksCaching(configuration);
        }

        if (options.UseDistributedLocking)
        {
            services.AddBuildingBlocksDistributedLocking(configuration);
        }

        if (options.UseMessaging)
        {
            services.AddBuildingBlocksMessaging(configuration, options.ConsumersAssembly);
        }

        if (options.UseSmsServices)
        {
            services.AddBuildingBlocksSms(configuration);
        }

        if (options.UseHealthChecks)
        {
            services.AddBuildingBlocksHealthChecks(configuration);
        }

        if (options.UseRateLimiting)
        {
            services.AddBuildingBlocksRateLimiting(
                options.RateLimitPermitLimit,
                options.RateLimitWindowSeconds);
        }

        return builder;
    }

    public static WebApplication UseBuildingBlocksDefaults(
        this WebApplication app,
        Action<BuildingBlocksDefaultsOptions>? configure = null)
    {
        var options = CreateOptions(app, configure);

        if (options.UseExceptionHandling)
        {
            app.UseBuildingBlocksExceptionHandling();
        }

        if (options.UseLogging)
        {
            app.UseBuildingBlocksRequestLogging();
        }

        if (options.UseJwtAuthentication)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        if (options.UseRateLimiting)
        {
            app.UseBuildingBlocksRateLimiting();
        }

        if (options.UseHealthChecks)
        {
            app.UseBuildingBlocksHealthChecks();
        }

        return app;
    }

    private static BuildingBlocksDefaultsOptions CreateOptions(
        WebApplicationBuilder builder,
        Action<BuildingBlocksDefaultsOptions>? configure)
    {
        var options = new BuildingBlocksDefaultsOptions
        {
            ApplicationName = builder.Environment.ApplicationName
        };

        configure?.Invoke(options);
        options.ApplicationName ??= builder.Environment.ApplicationName;
        return options;
    }

    private static BuildingBlocksDefaultsOptions CreateOptions(
        WebApplication app,
        Action<BuildingBlocksDefaultsOptions>? configure)
    {
        var options = new BuildingBlocksDefaultsOptions
        {
            ApplicationName = app.Environment.ApplicationName
        };

        configure?.Invoke(options);
        options.ApplicationName ??= app.Environment.ApplicationName;
        return options;
    }
}
