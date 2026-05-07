using System.Security.Claims;
using ApiGateway.Configuration;
using ApiGateway.Models;
using ApiGateway.Observability;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using BuildingBlocks.CrossCutting.Caching.Models;
using Microsoft.Extensions.Options;

namespace ApiGateway.Middleware;

public class GatewayCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCacheService _cacheService;
    private readonly ILogger<GatewayCacheMiddleware> _logger;
    private readonly GatewayOptions _options;
    private readonly GatewayMetrics _metrics;
    
    public GatewayCacheMiddleware(
        RequestDelegate next,
        IDistributedCacheService cacheService,
        ILogger<GatewayCacheMiddleware> logger,
        IOptions<GatewayOptions> options,
        GatewayMetrics metrics)
    {
        _next = next;
        _cacheService = cacheService;
        _logger = logger;
        _options = options.Value;
        _metrics = metrics;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var route = context.GetEndpoint()?.Metadata.GetMetadata<RouteConfig>();
        
        if (route?.Cache?.Enabled != true || context.Request.Method != "GET")
        {
            await _next(context);
            return;
        }
        
        var cacheKey = GenerateCacheKey(context, route.Cache);
        
        var cachedResponse = await _cacheService.GetAsync<CachedResponse>(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogInformation("Gateway cache hit for {Path}", context.Request.Path);
            context.Response.Headers["X-Cache-Status"] = "HIT";
            
            // Record cache hit metric
            _metrics.RecordCacheHit(route.RouteId);
            
            // Record cache hit event in trace
            var activity = System.Diagnostics.Activity.Current;
            activity?.RecordCacheHit(cacheKey);
            
            await WriteCachedResponse(context, cachedResponse);
            return;
        }
        
        context.Response.Headers["X-Cache-Status"] = "MISS";
        
        // Record cache miss metric
        _metrics.RecordCacheMiss(route.RouteId);
        
        // Record cache miss event in trace
        var currentActivity = System.Diagnostics.Activity.Current;
        currentActivity?.RecordCacheMiss(cacheKey);
        
        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        await _next(context);
        
        // Cache successful responses
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            
            var cacheEntry = new CachedResponse
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType ?? "application/json",
                Content = responseContent,
                Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            };
            
            var cacheOptions = new CacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(route.Cache.TtlSeconds))
            };
            
            await _cacheService.SetAsync(cacheKey, cacheEntry, cacheOptions);
            _logger.LogInformation("Cached response for {Path} with TTL {Ttl}s", 
                context.Request.Path, route.Cache.TtlSeconds);
        }
        
        // Write response to client
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
    
    private string GenerateCacheKey(HttpContext context, CachePolicy policy)
    {
        var keyParts = new List<string>
        {
            "gateway",
            context.Request.Path.Value ?? ""
        };
        
        // Vary by query parameters
        foreach (var param in policy.VaryByQueryParams)
        {
            if (context.Request.Query.TryGetValue(param, out var value))
            {
                keyParts.Add($"{param}={value}");
            }
        }
        
        // Vary by headers
        foreach (var header in policy.VaryByHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var value))
            {
                keyParts.Add($"{header}={value}");
            }
        }
        
        // Vary by user if authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value 
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                keyParts.Add($"user={userId}");
            }
        }
        
        return string.Join(":", keyParts);
    }
    
    private async Task WriteCachedResponse(HttpContext context, CachedResponse cached)
    {
        context.Response.StatusCode = cached.StatusCode;
        context.Response.ContentType = cached.ContentType;
        
        foreach (var header in cached.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }
        
        await context.Response.WriteAsync(cached.Content);
    }
}
