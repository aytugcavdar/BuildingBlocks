using BuildingBlocks.Core.Locking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BuildingBlocks.CrossCutting.Locking;

public static class LockingExtensions
{
    public static IServiceCollection AddDistributedLocking(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddBuildingBlocksDistributedLocking(configuration);
    }

    public static IServiceCollection AddBuildingBlocksDistributedLocking(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration.GetValue<string>("BuildingBlocks:Caching:RedisConnectionString")
            ?? configuration.GetValue<string>("CacheSettings:RedisConnectionString")
            ?? configuration.GetValue<string>("CacheSettings:RedisUrl")
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<IDistributedLock, RedisDistributedLockProvider>();

        return services;
    }
}
