namespace ApiGateway.Configuration;

public class GatewayOptions
{
    public List<RouteConfig> Routes { get; set; } = new();
    public AuthenticationConfig Authentication { get; set; } = new();
    public CacheConfig Cache { get; set; } = new();
    public ResilienceConfig Resilience { get; set; } = new();
    public LoadBalancingConfig LoadBalancing { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public ObservabilityConfig Observability { get; set; } = new();
}
