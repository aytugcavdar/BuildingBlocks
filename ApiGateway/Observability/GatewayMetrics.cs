using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ApiGateway.Observability;

/// <summary>
/// Provides custom metrics for gateway operations
/// </summary>
public class GatewayMetrics
{
    public const string MeterName = "ApiGateway";
    
    private readonly Meter _meter;
    private readonly Counter<long> _requestsTotal;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _downstreamCallsTotal;
    private readonly Counter<long> _cacheHitsTotal;
    private readonly Counter<long> _cacheMissesTotal;
    private readonly ObservableGauge<int> _circuitBreakerState;
    private readonly Counter<long> _rateLimitRejectionsTotal;
    
    // Circuit breaker state tracking (per service)
    private readonly Dictionary<string, int> _circuitBreakerStates = new();
    private readonly object _lock = new();
    
    public GatewayMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");
        
        // gateway_requests_total counter
        _requestsTotal = _meter.CreateCounter<long>(
            "gateway_requests_total",
            description: "Total number of requests processed by the gateway");
        
        // gateway_request_duration_seconds histogram
        _requestDuration = _meter.CreateHistogram<double>(
            "gateway_request_duration_seconds",
            unit: "s",
            description: "Duration of gateway requests in seconds");
        
        // gateway_downstream_calls_total counter
        _downstreamCallsTotal = _meter.CreateCounter<long>(
            "gateway_downstream_calls_total",
            description: "Total number of downstream service calls");
        
        // gateway_cache_hits_total counter
        _cacheHitsTotal = _meter.CreateCounter<long>(
            "gateway_cache_hits_total",
            description: "Total number of cache hits");
        
        // gateway_cache_misses_total counter
        _cacheMissesTotal = _meter.CreateCounter<long>(
            "gateway_cache_misses_total",
            description: "Total number of cache misses");
        
        // gateway_circuit_breaker_state gauge (0=closed, 1=half-open, 2=open)
        _circuitBreakerState = _meter.CreateObservableGauge<int>(
            "gateway_circuit_breaker_state",
            observeValues: ObserveCircuitBreakerStates,
            description: "Circuit breaker state (0=closed, 1=half-open, 2=open)");
        
        // gateway_rate_limit_rejections_total counter
        _rateLimitRejectionsTotal = _meter.CreateCounter<long>(
            "gateway_rate_limit_rejections_total",
            description: "Total number of rate limit rejections");
    }
    
    /// <summary>
    /// Records a request with route, method, and status code
    /// </summary>
    public void RecordRequest(string route, string method, int statusCode, double durationSeconds)
    {
        var tags = new TagList
        {
            { "route", route },
            { "method", method },
            { "status", statusCode.ToString() }
        };
        
        _requestsTotal.Add(1, tags);
        _requestDuration.Record(durationSeconds, tags);
    }
    
    /// <summary>
    /// Records a downstream service call
    /// </summary>
    public void RecordDownstreamCall(string serviceName, string method, int statusCode)
    {
        var tags = new TagList
        {
            { "service", serviceName },
            { "method", method },
            { "status", statusCode.ToString() }
        };
        
        _downstreamCallsTotal.Add(1, tags);
    }
    
    /// <summary>
    /// Records a cache hit
    /// </summary>
    public void RecordCacheHit(string route)
    {
        var tags = new TagList
        {
            { "route", route }
        };
        
        _cacheHitsTotal.Add(1, tags);
    }
    
    /// <summary>
    /// Records a cache miss
    /// </summary>
    public void RecordCacheMiss(string route)
    {
        var tags = new TagList
        {
            { "route", route }
        };
        
        _cacheMissesTotal.Add(1, tags);
    }
    
    /// <summary>
    /// Updates circuit breaker state for a service
    /// 0 = Closed (healthy)
    /// 1 = Half-Open (testing)
    /// 2 = Open (failing)
    /// </summary>
    public void UpdateCircuitBreakerState(string serviceName, CircuitBreakerState state)
    {
        lock (_lock)
        {
            _circuitBreakerStates[serviceName] = (int)state;
        }
    }
    
    /// <summary>
    /// Records a rate limit rejection
    /// </summary>
    public void RecordRateLimitRejection(string route, string partitionBy)
    {
        var tags = new TagList
        {
            { "route", route },
            { "partition_by", partitionBy }
        };
        
        _rateLimitRejectionsTotal.Add(1, tags);
    }
    
    /// <summary>
    /// Observes circuit breaker states for all services
    /// </summary>
    private IEnumerable<Measurement<int>> ObserveCircuitBreakerStates()
    {
        lock (_lock)
        {
            foreach (var kvp in _circuitBreakerStates)
            {
                yield return new Measurement<int>(
                    kvp.Value,
                    new TagList { { "service", kvp.Key } });
            }
        }
    }
}

/// <summary>
/// Circuit breaker state enumeration
/// </summary>
public enum CircuitBreakerState
{
    Closed = 0,
    HalfOpen = 1,
    Open = 2
}
