using System.Diagnostics;

namespace ApiGateway.Observability;

/// <summary>
/// Provides custom ActivitySource for gateway-specific operations
/// </summary>
public static class GatewayActivitySource
{
    public const string SourceName = "ApiGateway";
    
    private static readonly ActivitySource _activitySource = new(SourceName, "1.0.0");
    
    /// <summary>
    /// Gets the ActivitySource for creating custom spans
    /// </summary>
    public static ActivitySource Source => _activitySource;
    
    /// <summary>
    /// Creates a new activity for a gateway operation
    /// </summary>
    public static Activity? StartActivity(
        string operationName,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext parentContext = default,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        var activity = parentContext == default
            ? _activitySource.StartActivity(operationName, kind)
            : _activitySource.StartActivity(operationName, kind, parentContext);
        
        if (activity != null && tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }
        
        return activity;
    }
    
    /// <summary>
    /// Adds standard gateway tags to an activity
    /// </summary>
    public static void AddGatewayTags(
        this Activity? activity,
        string? serviceName = null,
        string? httpMethod = null,
        string? path = null,
        int? statusCode = null,
        string? routeId = null)
    {
        if (activity == null) return;
        
        if (!string.IsNullOrEmpty(serviceName))
            activity.SetTag("gateway.service_name", serviceName);
        
        if (!string.IsNullOrEmpty(httpMethod))
            activity.SetTag("http.method", httpMethod);
        
        if (!string.IsNullOrEmpty(path))
            activity.SetTag("http.path", path);
        
        if (statusCode.HasValue)
            activity.SetTag("http.status_code", statusCode.Value);
        
        if (!string.IsNullOrEmpty(routeId))
            activity.SetTag("gateway.route_id", routeId);
    }
    
    /// <summary>
    /// Records a cache hit event
    /// </summary>
    public static void RecordCacheHit(this Activity? activity, string cacheKey)
    {
        activity?.AddEvent(new ActivityEvent("cache.hit", tags: new ActivityTagsCollection
        {
            { "cache.key", cacheKey }
        }));
    }
    
    /// <summary>
    /// Records a cache miss event
    /// </summary>
    public static void RecordCacheMiss(this Activity? activity, string cacheKey)
    {
        activity?.AddEvent(new ActivityEvent("cache.miss", tags: new ActivityTagsCollection
        {
            { "cache.key", cacheKey }
        }));
    }
    
    /// <summary>
    /// Records a circuit breaker open event
    /// </summary>
    public static void RecordCircuitBreakerOpen(this Activity? activity, string serviceName)
    {
        activity?.AddEvent(new ActivityEvent("circuit_breaker.open", tags: new ActivityTagsCollection
        {
            { "service.name", serviceName }
        }));
    }
    
    /// <summary>
    /// Records a rate limit rejection event
    /// </summary>
    public static void RecordRateLimitRejection(this Activity? activity, string routeId, string partitionKey)
    {
        activity?.AddEvent(new ActivityEvent("rate_limit.rejected", tags: new ActivityTagsCollection
        {
            { "route.id", routeId },
            { "partition.key", partitionKey }
        }));
    }
}
