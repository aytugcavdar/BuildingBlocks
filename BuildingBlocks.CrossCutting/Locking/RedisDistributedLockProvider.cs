using BuildingBlocks.Core.Locking;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace BuildingBlocks.CrossCutting.Locking;

public class RedisDistributedLockHandle : IDistributedLockHandle
{
    private readonly IAsyncDisposable _medallionHandle;

    public RedisDistributedLockHandle(IAsyncDisposable medallionHandle)
    {
        _medallionHandle = medallionHandle;
    }

    public async ValueTask DisposeAsync()
    {
        if (_medallionHandle != null)
        {
            await _medallionHandle.DisposeAsync();
        }
    }

    public void Dispose()
    {
        // Senkron dispose fallback
        _medallionHandle?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

public class RedisDistributedLockProvider : BuildingBlocks.Core.Locking.IDistributedLock
{
    private readonly IDatabase _redisDatabase;

    public RedisDistributedLockProvider(IConnectionMultiplexer connectionMultiplexer)
    {
        _redisDatabase = connectionMultiplexer.GetDatabase();
    }

    public async Task<IDistributedLockHandle> AcquireAsync(string name, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        var redisLock = new RedisDistributedLock(name, _redisDatabase);
        var handle = await redisLock.AcquireAsync(timeout, cancellationToken);
        return new RedisDistributedLockHandle(handle);
    }

    public async Task<IDistributedLockHandle?> TryAcquireAsync(string name, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        var redisLock = new RedisDistributedLock(name, _redisDatabase);
        var handle = await redisLock.TryAcquireAsync(timeout, cancellationToken);
        
        if (handle == null)
            return null;

        return new RedisDistributedLockHandle(handle);
    }
}
