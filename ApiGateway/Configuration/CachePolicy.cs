namespace ApiGateway.Configuration;

public class CachePolicy
{
    public bool Enabled { get; set; } = false;
    public int TtlSeconds { get; set; } = 60;
    public List<string> VaryByHeaders { get; set; } = new();
    public List<string> VaryByQueryParams { get; set; } = new();
}
