namespace ApiGateway.Services;

public class ServiceResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
}
