using ApiGateway.Configuration;
using ApiGateway.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ApiGateway.Health;

public class DownstreamHealthCheck : IHealthCheck
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GatewayOptions _options;
    private readonly ILogger<DownstreamHealthCheck> _logger;
    
    public DownstreamHealthCheck(
        IServiceDiscovery serviceDiscovery,
        IHttpClientFactory httpClientFactory,
        IOptions<GatewayOptions> options,
        ILogger<DownstreamHealthCheck> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var serviceNames = _options.Routes
            .Where(r => r.Enabled)
            .Select(r => ExtractServiceName(r.DownstreamServiceUrl))
            .Distinct()
            .ToList();
        
        var healthResults = new Dictionary<string, object>();
        var allHealthy = true;
        var anyDegraded = false;
        
        foreach (var serviceName in serviceNames)
        {
            try
            {
                var serviceUrl = await _serviceDiscovery.ResolveServiceUrlAsync(serviceName, cancellationToken);
                var healthUrl = $"{serviceUrl}/health/ready";
                
                var client = _httpClientFactory.CreateClient("health");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await client.GetAsync(healthUrl, cts.Token);
                
                var isHealthy = response.IsSuccessStatusCode;
                healthResults[serviceName] = new
                {
                    status = isHealthy ? "Healthy" : "Unhealthy",
                    statusCode = (int)response.StatusCode,
                    url = healthUrl
                };
                
                if (!isHealthy)
                {
                    allHealthy = false;
                    anyDegraded = true;
                    _logger.LogWarning("Downstream service {ServiceName} is unhealthy", serviceName);
                }
            }
            catch (Exception ex)
            {
                healthResults[serviceName] = new
                {
                    status = "Unhealthy",
                    error = ex.Message
                };
                allHealthy = false;
                anyDegraded = true;
                _logger.LogError(ex, "Failed to check health of downstream service {ServiceName}", serviceName);
            }
        }
        
        var status = allHealthy ? HealthStatus.Healthy : 
                     anyDegraded ? HealthStatus.Degraded : 
                     HealthStatus.Unhealthy;
        
        return new HealthCheckResult(
            status,
            description: $"Checked {serviceNames.Count} downstream services",
            data: healthResults);
    }
    
    private string ExtractServiceName(string serviceUrl)
    {
        // Extract service name from URL
        // Examples:
        // - "http://user-service" -> "user-service"
        // - "http://user-service.default.svc.cluster.local" -> "user-service"
        // - "http://localhost:5001" -> "localhost"
        
        if (string.IsNullOrEmpty(serviceUrl))
            return "unknown";
        
        try
        {
            var uri = new Uri(serviceUrl);
            var host = uri.Host;
            
            // For Kubernetes DNS, extract service name (first part before dot)
            var parts = host.Split('.');
            return parts[0];
        }
        catch
        {
            return serviceUrl;
        }
    }
}
