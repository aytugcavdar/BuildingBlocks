using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ApiGateway.Health;

public class GatewayHealthCheck : IHealthCheck
{
    private readonly ILogger<GatewayHealthCheck> _logger;
    
    public GatewayHealthCheck(ILogger<GatewayHealthCheck> logger)
    {
        _logger = logger;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check gateway's own health
            var data = new Dictionary<string, object>
            {
                ["status"] = "Healthy",
                ["timestamp"] = DateTime.UtcNow
            };
            
            // Check memory usage
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / 1024 / 1024;
            data["memoryUsageMB"] = memoryMB;
            
            // Check if memory usage is too high (> 1GB)
            if (memoryMB > 1024)
            {
                _logger.LogWarning("Gateway memory usage is high: {MemoryMB}MB", memoryMB);
                return Task.FromResult(HealthCheckResult.Degraded(
                    "High memory usage",
                    data: data));
            }
            
            return Task.FromResult(HealthCheckResult.Healthy(
                "Gateway is healthy",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Gateway health check failed",
                ex));
        }
    }
}
