namespace ApiGateway.Services;

public class KubernetesServiceDiscovery : IServiceDiscovery
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KubernetesServiceDiscovery> _logger;
    private readonly Dictionary<string, string> _serviceUrlCache = new();
    
    public KubernetesServiceDiscovery(
        IConfiguration configuration,
        ILogger<KubernetesServiceDiscovery> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public Task<string> ResolveServiceUrlAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_serviceUrlCache.TryGetValue(serviceName, out var cachedUrl))
        {
            return Task.FromResult(cachedUrl);
        }
        
        // Check configuration for static URL
        var staticUrl = _configuration[$"Gateway:Services:{serviceName}:Url"];
        if (!string.IsNullOrEmpty(staticUrl))
        {
            _serviceUrlCache[serviceName] = staticUrl;
            _logger.LogInformation("Resolved service {ServiceName} to static URL {Url}", serviceName, staticUrl);
            return Task.FromResult(staticUrl);
        }
        
        // Use Kubernetes DNS (service-name.namespace.svc.cluster.local)
        var @namespace = _configuration["Gateway:Services:DefaultNamespace"] ?? "default";
        var port = _configuration[$"Gateway:Services:{serviceName}:Port"] ?? "80";
        var kubernetesUrl = $"http://{serviceName}.{@namespace}.svc.cluster.local:{port}";
        
        _serviceUrlCache[serviceName] = kubernetesUrl;
        _logger.LogInformation("Resolved service {ServiceName} to Kubernetes DNS {Url}", serviceName, kubernetesUrl);
        
        return Task.FromResult(kubernetesUrl);
    }
    
    public Task<List<ServiceInstance>> GetServiceInstancesAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        // In Kubernetes, service discovery is handled by kube-proxy
        // This method would be used for custom load balancing logic
        // For now, return a single instance representing the K8s service
        var url = ResolveServiceUrlAsync(serviceName, cancellationToken).Result;
        var uri = new Uri(url);
        
        var instance = new ServiceInstance
        {
            Host = uri.Host,
            Port = uri.Port,
            IsHealthy = true
        };
        
        return Task.FromResult(new List<ServiceInstance> { instance });
    }
}
