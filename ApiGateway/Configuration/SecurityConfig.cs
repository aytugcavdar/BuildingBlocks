namespace ApiGateway.Configuration;

public class SecurityConfig
{
    public bool EnableSecurityHeaders { get; set; } = true;
    public bool EnableCors { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new();
    public bool AllowCredentials { get; set; } = false;
    public int MaxRequestBodySizeBytes { get; set; } = 10485760; // 10MB
}
