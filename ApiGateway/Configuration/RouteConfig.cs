namespace ApiGateway.Configuration;

public class RouteConfig
{
    public string RouteId { get; set; } = string.Empty;
    public string UpstreamPathPattern { get; set; } = string.Empty;
    public string DownstreamServiceUrl { get; set; } = string.Empty;
    public string DownstreamPathTemplate { get; set; } = string.Empty;
    public List<string> HttpMethods { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public bool RequireAuthentication { get; set; } = true;
    public List<string> RequiredRoles { get; set; } = new();
    public List<string> RequiredPolicies { get; set; } = new();
    public RateLimitPolicy? RateLimit { get; set; }
    public CachePolicy? Cache { get; set; }
    public TransformationRules? Transformation { get; set; }
    public bool IsAggregation { get; set; } = false;
    public List<string>? AggregationTargets { get; set; }
    public string? ClientType { get; set; } // For BFF: "web", "mobile", "desktop"
    public string? ApiVersion { get; set; } // API version (e.g., "v1", "v2")
    public bool IsDefaultVersion { get; set; } = false; // Whether this is the default version when no version is specified
}
