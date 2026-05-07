using BuildingBlocks.Core.Caching;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using BuildingBlocks.CrossCutting.Caching.Models;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.CrossCutting.Caching;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableRequest
{
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private readonly Configuration.CacheSettings _cacheSettings;

    public CachingBehavior(IDistributedCacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger, IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        _cacheSettings = configuration.GetSection("CacheSettings").Get<Configuration.CacheSettings>() ?? new Configuration.CacheSettings();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
            return await next();

        var cachedResponse = await _cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);
        if (cachedResponse != null)
        {
            _logger.LogInformation("🚀 Fetched from Cache -> '{CacheKey}'", request.CacheKey);
            return cachedResponse;
        }

        return await GetResponseAndAddToCache(request, next, cancellationToken);
    }

    private async Task<TResponse> GetResponseAndAddToCache(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        var slidingExpiration = request.SlidingExpiration ?? TimeSpan.FromSeconds(_cacheSettings.DefaultTtlSeconds);
        
        var cacheOptions = new CacheEntryOptions
        {
            SlidingExpiration = slidingExpiration
        };

        // Map CacheGroupKey to Tags
        if (request.CacheGroupKey != null)
        {
            cacheOptions.Tags.Add(request.CacheGroupKey);
        }

        await _cache.SetAsync(request.CacheKey, response, cacheOptions, cancellationToken);
        
        _logger.LogInformation("✅ Added to Cache -> '{CacheKey}'", request.CacheKey);

        return response;
    }
}
