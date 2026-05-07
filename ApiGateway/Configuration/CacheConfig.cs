namespace ApiGateway.Configuration;

public class CacheConfig
{
    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultTtlSeconds { get; set; } = 300;
}
