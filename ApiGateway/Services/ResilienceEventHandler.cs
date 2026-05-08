using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace ApiGateway.Services;

/// <summary>
/// Handles resilience policy events with proper dependency injection
/// </summary>
public class ResilienceEventHandler
{
    private readonly ILogger<ResilienceEventHandler> _logger;

    public ResilienceEventHandler(ILogger<ResilienceEventHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask OnRetryAsync(OnRetryArguments<HttpResponseMessage> args)
    {
        _logger.LogWarning(
            "Retry attempt {AttemptNumber} for {Method} {Uri} after {Delay}ms. Reason: {Outcome}",
            args.AttemptNumber,
            args.Outcome.Result?.RequestMessage?.Method,
            args.Outcome.Result?.RequestMessage?.RequestUri,
            args.RetryDelay.TotalMilliseconds,
            args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
        
        return ValueTask.CompletedTask;
    }

    public ValueTask OnCircuitBreakerOpenedAsync(OnCircuitOpenedArguments<HttpResponseMessage> args)
    {
        _logger.LogError(
            "Circuit breaker opened for downstream service. Break duration: {BreakDuration}s. Reason: {Outcome}",
            args.BreakDuration.TotalSeconds,
            args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
        
        return ValueTask.CompletedTask;
    }

    public ValueTask OnCircuitBreakerClosedAsync(OnCircuitClosedArguments<HttpResponseMessage> args)
    {
        _logger.LogInformation("Circuit breaker closed for downstream service. Service is healthy again.");
        return ValueTask.CompletedTask;
    }

    public void OnCircuitBreakerHalfOpened()
    {
        _logger.LogInformation("Circuit breaker half-opened for downstream service. Testing if service recovered.");
    }
}
