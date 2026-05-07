namespace ApiGateway.Configuration;

public class ObservabilityConfig
{
    public bool EnableDetailedLogging { get; set; } = false;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableTracing { get; set; } = true;
    public bool LogRequestBodies { get; set; } = false;
    public bool LogResponseBodies { get; set; } = false;
}
