namespace ApiGateway.Models;

public class CachedResponse
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}
