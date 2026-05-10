using BuildingBlocks.CrossCutting.Caching.Configuration;
using BuildingBlocks.CrossCutting.Caching.Core;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using BuildingBlocks.CrossCutting.Caching.Providers;
using BuildingBlocks.CrossCutting.Caching.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BuildingBlocks.CrossCutting.Tests.Caching;

public class MultiLevelCacheServiceTests
{
    [Fact]
    public async Task GetAsync_ShouldReadCompressedValue_WhenOnlyMemoryCacheIsEnabled()
    {
        var settings = Options.Create(new CacheSettings
        {
            EnableL1Cache = true,
            EnableL2Cache = false,
            EnableCompression = true,
            CompressionThresholdBytes = 1,
            DefaultTtlSeconds = 300,
            MemoryCacheSizeLimitMb = 100,
            OperationTimeoutSeconds = 5,
            WarmupConcurrencyLimit = 1
        });

        using var memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100 * 1024 * 1024
        });

        ICacheProvider provider = new MemoryCacheProvider(memoryCache);
        var service = new MultiLevelCacheService(
            settings,
            new[] { provider },
            Substitute.For<ILogger<MultiLevelCacheService>>(),
            new CacheKeyGenerator(),
            new JsonCacheSerializer());

        var value = new string('a', 4096);

        await service.SetAsync("compressed-key", value);
        var result = await service.GetAsync<string>("compressed-key");

        result.Should().Be(value);
    }
}
