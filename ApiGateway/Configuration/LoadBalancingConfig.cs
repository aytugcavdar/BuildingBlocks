namespace ApiGateway.Configuration;

public class LoadBalancingConfig
{
    public string Strategy { get; set; } = "RoundRobin"; // "RoundRobin", "LeastConnections", "Weighted"
    public bool EnableHealthChecks { get; set; } = true;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
}
