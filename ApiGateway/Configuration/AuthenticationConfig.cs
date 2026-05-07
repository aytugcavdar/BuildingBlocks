namespace ApiGateway.Configuration;

public class AuthenticationConfig
{
    public bool Enabled { get; set; } = true;
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public int ClockSkewSeconds { get; set; } = 300;
}
