namespace ApiGateway.Services;

public class AggregatedResponse
{
    public Dictionary<string, ServiceResponse> Responses { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public bool HasErrors { get; set; }
}
