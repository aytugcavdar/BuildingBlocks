namespace ApiGateway.Services;

public class AggregationRequest
{
    public List<DownstreamRequest> Requests { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 30;
    public bool FailOnAnyError { get; set; } = false;
}
