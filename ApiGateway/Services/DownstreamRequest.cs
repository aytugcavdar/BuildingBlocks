namespace ApiGateway.Services;

public class DownstreamRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public Dictionary<string, string> Headers { get; set; } = new();
    public object? Body { get; set; }
}
