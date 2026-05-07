using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace BuildingBlocks.CrossCutting.HttpResilience;

/// <summary>
/// HTTP client resilience extensions using Polly v8
/// </summary>
public static class HttpClientResilienceExtensions
{
    /// <summary>
    /// Adds standard resilience handler (Retry, Circuit Breaker, Timeout, Hedging)
    /// </summary>
    public static IHttpClientBuilder AddStandardResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler();
        return builder;
    }

    /// <summary>
    /// Adds custom resilience pipeline with configurable retry and circuit breaker
    /// </summary>
    public static IHttpClientBuilder AddCustomRetryAndCircuitBreaker(
        this IHttpClientBuilder builder, 
        int retryCount = 3, 
        double circuitBreakerFailureRatio = 0.5)
    {
        // Use standard resilience handler with custom options
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = retryCount;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            
            options.CircuitBreaker.FailureRatio = circuitBreakerFailureRatio;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        return builder;
    }
}
