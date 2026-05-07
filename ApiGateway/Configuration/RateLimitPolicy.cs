namespace ApiGateway.Configuration;

public class RateLimitPolicy
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public string PartitionBy { get; set; } = "user"; // "user", "ip", "global"
}
