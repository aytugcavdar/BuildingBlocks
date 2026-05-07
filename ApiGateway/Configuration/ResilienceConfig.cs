namespace ApiGateway.Configuration;

public class ResilienceConfig
{
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 30;
}
