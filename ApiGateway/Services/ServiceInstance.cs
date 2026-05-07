namespace ApiGateway.Services;

public class ServiceInstance
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool IsHealthy { get; set; } = true;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
