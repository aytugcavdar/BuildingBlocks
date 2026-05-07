using System.Diagnostics;
using ApiGateway.Configuration;
using ApiGateway.Observability;
using Microsoft.Extensions.Options;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware that records gateway metrics for all requests
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewayMetrics _metrics;
    private readonly ILogger<MetricsMiddleware> _logger;
    private readonly GatewayOptions _options;
    
    public MetricsMiddleware(
        RequestDelegate next,
        GatewayMetrics metrics,
        ILogger<MetricsMiddleware> logger,
        IOptions<GatewayOptions> options)
    {
        _next = next;
        _metrics = metrics;
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        
        // Get route information
        var endpoint = context.GetEndpoint();
        var metadata = endpoint?.Metadata.GetMetadata<IDictionary<string, string>>();
        var routeId = metadata != null && metadata.ContainsKey("RouteId") 
            ? metadata["RouteId"] 
            : "unknown";
        
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            
            // Record request metrics
            _metrics.RecordRequest(
                route: routeId,
                method: context.Request.Method,
                statusCode: context.Response.StatusCode,
                durationSeconds: sw.Elapsed.TotalSeconds);
        }
    }
}
