using ApiGateway.Configuration;
using ApiGateway.Observability;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware that applies per-route rate limiting based on route metadata
/// </summary>
public class PerRouteRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerRouteRateLimitingMiddleware> _logger;
    private readonly GatewayMetrics _metrics;
    private readonly Dictionary<string, RateLimiter> _rateLimiters = new();
    private readonly object _lock = new();

    public PerRouteRateLimitingMiddleware(
        RequestDelegate next,
        ILogger<PerRouteRateLimitingMiddleware> logger,
        GatewayMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get route metadata
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var metadata = endpoint.Metadata.GetMetadata<IDictionary<string, string>>();
        if (metadata == null || !metadata.ContainsKey("RateLimit.PermitLimit"))
        {
            // No rate limiting configured for this route
            await _next(context);
            return;
        }

        // Extract rate limit configuration from metadata
        var permitLimit = int.Parse(metadata["RateLimit.PermitLimit"]);
        var windowSeconds = int.Parse(metadata["RateLimit.WindowSeconds"]);
        var partitionBy = metadata["RateLimit.PartitionBy"];
        var routeId = metadata["RouteId"];

        // Get partition key based on partition strategy
        var partitionKey = GetPartitionKey(context, partitionBy, routeId);

        // Get or create rate limiter for this route and partition
        var rateLimiter = GetOrCreateRateLimiter(routeId, partitionKey, permitLimit, windowSeconds);

        // Attempt to acquire a permit
        using var lease = await rateLimiter.AcquireAsync(permitCount: 1, context.RequestAborted);

        if (!lease.IsAcquired)
        {
            // Rate limit exceeded
            _logger.LogWarning(
                "Rate limit exceeded for route {RouteId}, partition {PartitionKey}",
                routeId,
                partitionKey);
            
            // Record rate limit rejection metric
            _metrics.RecordRateLimitRejection(routeId, partitionBy);
            
            // Record rate limit rejection event in trace
            var activity = System.Diagnostics.Activity.Current;
            activity?.RecordRateLimitRejection(routeId, partitionKey);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            // Add Retry-After header (Requirement 6.6)
            var retryAfterSeconds = windowSeconds;
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

            var problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = 429,
                detail = $"Rate limit exceeded. Please retry after {retryAfterSeconds} seconds.",
                instance = context.Request.Path.Value,
                traceId = context.TraceIdentifier,
                retryAfter = retryAfterSeconds
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
            return;
        }

        // Permit acquired, continue to next middleware
        await _next(context);
    }

    /// <summary>
    /// Gets the partition key based on the partition strategy
    /// </summary>
    private string GetPartitionKey(HttpContext context, string partitionBy, string routeId)
    {
        return partitionBy.ToLowerInvariant() switch
        {
            "user" => GetUserPartitionKey(context, routeId),
            "ip" => GetIpPartitionKey(context, routeId),
            "global" => $"global:{routeId}",
            _ => $"global:{routeId}"
        };
    }

    /// <summary>
    /// Gets partition key based on authenticated user
    /// </summary>
    private string GetUserPartitionKey(HttpContext context, string routeId)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}:{routeId}";
            }
        }

        // Fall back to IP if user is not authenticated
        return GetIpPartitionKey(context, routeId);
    }

    /// <summary>
    /// Gets partition key based on client IP address
    /// </summary>
    private string GetIpPartitionKey(HttpContext context, string routeId)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}:{routeId}";
    }

    /// <summary>
    /// Gets or creates a rate limiter for the given route and partition
    /// </summary>
    private RateLimiter GetOrCreateRateLimiter(
        string routeId,
        string partitionKey,
        int permitLimit,
        int windowSeconds)
    {
        var key = $"{routeId}:{partitionKey}";

        if (_rateLimiters.TryGetValue(key, out var existingLimiter))
        {
            return existingLimiter;
        }

        lock (_lock)
        {
            // Double-check after acquiring lock
            if (_rateLimiters.TryGetValue(key, out existingLimiter))
            {
                return existingLimiter;
            }

            // Create new rate limiter with fixed window strategy
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queuing
            });

            _rateLimiters[key] = limiter;

            _logger.LogInformation(
                "Created rate limiter for route {RouteId}, partition {PartitionKey} with limit {PermitLimit}/{WindowSeconds}s",
                routeId,
                partitionKey,
                permitLimit,
                windowSeconds);

            return limiter;
        }
    }
}
