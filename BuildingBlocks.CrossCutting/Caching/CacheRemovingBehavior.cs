using BuildingBlocks.Core.Caching;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.CrossCutting.Caching;

public class CacheRemovingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheRemoverRequest
{
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<CacheRemovingBehavior<TRequest, TResponse>> _logger;

    public CacheRemovingBehavior(IDistributedCacheService cache, ILogger<CacheRemovingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
            return await next();

        var response = await next();

        if (request.CacheGroupKey != null)
        {
            var removedCount = await _cache.RemoveByTagAsync(request.CacheGroupKey, cancellationToken);
            _logger.LogInformation("🗑️ Cache Group '{CacheGroupKey}' cleaned, removed {Count} keys", request.CacheGroupKey, removedCount);
        }

        if (request.CacheKey != null)
        {
            await _cache.RemoveAsync(request.CacheKey, cancellationToken);
            _logger.LogInformation("🗑️ Removed from Cache -> '{CacheKey}'", request.CacheKey);
        }

        return response;
    }
}
