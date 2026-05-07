using BuildingBlocks.Core.Locking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BuildingBlocks.CrossCutting.Locking;

public static class LockingExtensions
{
    public static IServiceCollection AddDistributedLocking(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") 
            ?? configuration.GetValue<string>("CacheSettings:RedisUrl") 
            ?? "localhost:6379";

        // StackExchange.Redis bağlantısı Singleton olarak kaydedilir
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Kilit mekanizması kaydedilir
        services.AddSingleton<IDistributedLock, RedisDistributedLockProvider>();

        return services;
    }
}
