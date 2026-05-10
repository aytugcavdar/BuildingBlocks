using BuildingBlocks.CrossCutting.Caching.Extensions;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace BuildingBlocks.CrossCutting.Tests.Caching;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksCaching_ShouldUseBuildingBlocksCachingSection()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("BuildingBlocks:Caching");

        services.AddBuildingBlocksCaching(configuration);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDistributedCacheService>().Should().NotBeNull();
        provider.GetServices<ICacheProvider>()
            .Select(cacheProvider => cacheProvider.ProviderName)
            .Should()
            .Equal("Memory");
    }

    [Fact]
    public void AddBuildingBlocksCaching_ShouldFallbackToLegacyCacheSettingsSection()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("CacheSettings");

        services.AddBuildingBlocksCaching(configuration);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDistributedCacheService>().Should().NotBeNull();
        provider.GetServices<ICacheProvider>()
            .Select(cacheProvider => cacheProvider.ProviderName)
            .Should()
            .Equal("Memory");
    }

    private static IConfiguration CreateConfiguration(string sectionName)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{sectionName}:EnableL1Cache"] = "true",
            [$"{sectionName}:EnableL2Cache"] = "false",
            [$"{sectionName}:EnableCompression"] = "true",
            [$"{sectionName}:CompressionThresholdBytes"] = "1024",
            [$"{sectionName}:DefaultTtlSeconds"] = "300",
            [$"{sectionName}:MemoryCacheSizeLimitMb"] = "64",
            [$"{sectionName}:OperationTimeoutSeconds"] = "5",
            [$"{sectionName}:WarmupConcurrencyLimit"] = "1"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
