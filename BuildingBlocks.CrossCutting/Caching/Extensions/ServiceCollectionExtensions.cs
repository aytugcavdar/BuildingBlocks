using BuildingBlocks.CrossCutting.Caching.Configuration;
using BuildingBlocks.CrossCutting.Caching.Core;
using BuildingBlocks.CrossCutting.Caching.Exceptions;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using BuildingBlocks.CrossCutting.Caching.Providers;
using BuildingBlocks.CrossCutting.Caching.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BuildingBlocks.CrossCutting.Caching.Extensions;

/// <summary>
/// Extension methods for registering distributed caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds distributed caching with multi-level support (Memory + Redis).
    /// </summary>
    public static IServiceCollection AddDistributedCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddBuildingBlocksCaching(configuration, "CacheSettings");
    }

    /// <summary>
    /// Adds BuildingBlocks caching with multi-level support (Memory + Redis).
    /// Reads "BuildingBlocks:Caching" by default and falls back to "CacheSettings".
    /// </summary>
    public static IServiceCollection AddBuildingBlocksCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "BuildingBlocks:Caching")
    {
        // Bind configuration
        var section = GetSection(configuration, sectionName, "CacheSettings");
        var cacheSettings = section.Get<CacheSettings>() ?? new CacheSettings();
        services.Configure<CacheSettings>(section);

        // Validate configuration
        ValidateConfiguration(cacheSettings);

        // Register core components
        services.AddSingleton<CacheKeyGenerator>();
        services.AddSingleton<JsonCacheSerializer>();

        // Register Memory Cache (L1)
        if (cacheSettings.EnableL1Cache)
        {
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = cacheSettings.MemoryCacheSizeLimitMb * 1024 * 1024; // Convert MB to bytes
            });
            services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
        }

        // Register Redis (L2)
        if (cacheSettings.EnableL2Cache)
        {
            // Get connection string from environment variable or configuration
            var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                ?? cacheSettings.RedisConnectionString;

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new ConfigurationException("RedisConnectionString", "Redis connection string is required when L2 cache is enabled");
            }

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                try
                {
                    return ConnectionMultiplexer.Connect(redisConnectionString);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException("RedisConnectionString", $"Failed to connect to Redis: {ex.Message}", ex);
                }
            });

            services.AddSingleton<ICacheProvider, RedisCacheProvider>();
        }

        // Register cache service
        services.AddSingleton<IDistributedCacheService, MultiLevelCacheService>();

        return services;
    }

    private static IConfigurationSection GetSection(
        IConfiguration configuration,
        string preferredSectionName,
        string fallbackSectionName)
    {
        var preferred = configuration.GetSection(preferredSectionName);
        return preferred.Exists()
            ? preferred
            : configuration.GetSection(fallbackSectionName);
    }

    private static void ValidateConfiguration(CacheSettings settings)
    {
        if (settings.DefaultTtlSeconds < 0)
        {
            throw new ConfigurationException(nameof(settings.DefaultTtlSeconds),
                "Must be >= 0 (0 = infinite)");
        }

        if (settings.OperationTimeoutSeconds <= 0)
        {
            throw new ConfigurationException(nameof(settings.OperationTimeoutSeconds),
                "Must be > 0");
        }

        if (settings.CompressionThresholdBytes < 0)
        {
            throw new ConfigurationException(nameof(settings.CompressionThresholdBytes),
                "Must be >= 0");
        }

        if (settings.MemoryCacheSizeLimitMb <= 0)
        {
            throw new ConfigurationException(nameof(settings.MemoryCacheSizeLimitMb),
                "Must be > 0");
        }

        if (settings.WarmupConcurrencyLimit <= 0)
        {
            throw new ConfigurationException(nameof(settings.WarmupConcurrencyLimit),
                "Must be > 0");
        }

        if (!settings.EnableL1Cache && !settings.EnableL2Cache)
        {
            throw new ConfigurationException("CacheSettings",
                "At least one cache level (L1 or L2) must be enabled");
        }
    }
}
