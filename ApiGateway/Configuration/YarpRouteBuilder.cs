using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using GatewayRouteConfig = ApiGateway.Configuration.RouteConfig;
using YarpRouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;

namespace ApiGateway.Configuration;

/// <summary>
/// Builds YARP route and cluster configuration from GatewayOptions
/// </summary>
public class YarpRouteBuilder : IProxyConfigProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<YarpRouteBuilder> _logger;
    private volatile InMemoryConfigProvider _configProvider;

    public YarpRouteBuilder(
        IConfiguration configuration,
        ILogger<YarpRouteBuilder> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _configProvider = new InMemoryConfigProvider(Array.Empty<YarpRouteConfig>(), Array.Empty<ClusterConfig>());
        
        // Build initial configuration
        BuildConfiguration();
    }

    public IProxyConfig GetConfig()
    {
        return _configProvider.GetConfig();
    }

    /// <summary>
    /// Builds YARP configuration from GatewayOptions.Routes
    /// </summary>
    private void BuildConfiguration()
    {
        var gatewayOptions = new GatewayOptions();
        _configuration.GetSection("Gateway").Bind(gatewayOptions);

        var routes = new List<YarpRouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var route in gatewayOptions.Routes.Where(r => r.Enabled))
        {
            try
            {
                // Build YARP RouteConfig
                var yarpRoute = BuildYarpRoute(route);
                routes.Add(yarpRoute);

                // Build YARP ClusterConfig
                var yarpCluster = BuildYarpCluster(route, gatewayOptions);
                clusters.Add(yarpCluster);

                _logger.LogInformation(
                    "Built YARP route {RouteId}: {UpstreamPath} -> {DownstreamUrl}",
                    route.RouteId,
                    route.UpstreamPathPattern,
                    route.DownstreamServiceUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build YARP configuration for route {RouteId}", route.RouteId);
                throw new Exceptions.GatewayConfigurationException(
                    $"Failed to build YARP configuration for route {route.RouteId}: {ex.Message}",
                    ex);
            }
        }

        _configProvider = new InMemoryConfigProvider(routes, clusters);
        _logger.LogInformation("Built YARP configuration with {RouteCount} routes and {ClusterCount} clusters",
            routes.Count, clusters.Count);
    }

    /// <summary>
    /// Builds a YARP RouteConfig from a Gateway RouteConfig
    /// </summary>
    private YarpRouteConfig BuildYarpRoute(GatewayRouteConfig route)
    {
        var match = new RouteMatch
        {
            Path = route.UpstreamPathPattern,
            // YARP accepts null for Methods to match all HTTP methods
            // If specific methods are needed, they should be in separate routes
            Methods = route.HttpMethods?.Any() == true ? null : null
        };

        // Build metadata for middleware consumption
        var metadata = new Dictionary<string, string>
        {
            ["RouteId"] = route.RouteId,
            ["RequireAuthentication"] = route.RequireAuthentication.ToString(),
            ["TimeoutSeconds"] = route.TimeoutSeconds.ToString()
        };

        // Add authentication metadata
        if (route.RequireAuthentication)
        {
            metadata["AuthenticationSchemes"] = "Bearer";
        }

        // Add authorization metadata
        if (route.RequiredRoles.Any())
        {
            metadata["RequiredRoles"] = string.Join(",", route.RequiredRoles);
        }

        if (route.RequiredPolicies.Any())
        {
            metadata["RequiredPolicies"] = string.Join(",", route.RequiredPolicies);
        }

        // Add rate limiting metadata
        if (route.RateLimit != null)
        {
            metadata["RateLimit.PermitLimit"] = route.RateLimit.PermitLimit.ToString();
            metadata["RateLimit.WindowSeconds"] = route.RateLimit.WindowSeconds.ToString();
            metadata["RateLimit.PartitionBy"] = route.RateLimit.PartitionBy;
        }

        // Add caching metadata
        if (route.Cache?.Enabled == true)
        {
            metadata["Cache.Enabled"] = "true";
            metadata["Cache.TtlSeconds"] = route.Cache.TtlSeconds.ToString();
            if (route.Cache.VaryByHeaders.Any())
            {
                metadata["Cache.VaryByHeaders"] = string.Join(",", route.Cache.VaryByHeaders);
            }
            if (route.Cache.VaryByQueryParams.Any())
            {
                metadata["Cache.VaryByQueryParams"] = string.Join(",", route.Cache.VaryByQueryParams);
            }
        }

        // Add aggregation metadata
        if (route.IsAggregation)
        {
            metadata["IsAggregation"] = "true";
            if (route.AggregationTargets?.Any() == true)
            {
                metadata["AggregationTargets"] = string.Join(",", route.AggregationTargets);
            }
        }

        // Add BFF client type metadata
        if (!string.IsNullOrEmpty(route.ClientType))
        {
            metadata["ClientType"] = route.ClientType;
        }

        // Add API version metadata
        if (!string.IsNullOrEmpty(route.ApiVersion))
        {
            metadata["ApiVersion"] = route.ApiVersion;
            metadata["IsDefaultVersion"] = route.IsDefaultVersion.ToString();
        }

        // Build transforms for path and query parameters
        var transforms = BuildTransforms(route);

        return new YarpRouteConfig
        {
            RouteId = route.RouteId,
            ClusterId = $"{route.RouteId}-cluster",
            Match = match,
            Metadata = metadata,
            Transforms = transforms
        };
    }

    /// <summary>
    /// Builds YARP transforms for path and query parameter handling
    /// </summary>
    private List<IReadOnlyDictionary<string, string>> BuildTransforms(GatewayRouteConfig route)
    {
        var transforms = new List<IReadOnlyDictionary<string, string>>();

        // Path transform: rewrite upstream path to downstream path template
        if (!string.IsNullOrEmpty(route.DownstreamPathTemplate) &&
            route.DownstreamPathTemplate != route.UpstreamPathPattern)
        {
            // Use PathPattern transform to rewrite the path
            transforms.Add(new Dictionary<string, string>
            {
                ["PathPattern"] = route.DownstreamPathTemplate
            });
        }

        // Add custom request transformation rules if specified
        if (route.Transformation?.RequestHeaderMappings?.Any() == true)
        {
            foreach (var mapping in route.Transformation.RequestHeaderMappings)
            {
                transforms.Add(new Dictionary<string, string>
                {
                    ["RequestHeaderRemove"] = mapping.Key
                });
                transforms.Add(new Dictionary<string, string>
                {
                    ["RequestHeader"] = mapping.Value,
                    ["Set"] = $"{{{mapping.Key}}}"
                });
            }
        }

        return transforms;
    }

    /// <summary>
    /// Builds a YARP ClusterConfig from a Gateway RouteConfig
    /// </summary>
    private ClusterConfig BuildYarpCluster(
        GatewayRouteConfig route,
        GatewayOptions gatewayOptions)
    {
        var destinations = new Dictionary<string, DestinationConfig>();

        // For aggregation routes, we don't need destinations (handled by RequestAggregator)
        if (route.IsAggregation)
        {
            // Add a placeholder destination
            destinations["aggregation"] = new DestinationConfig
            {
                Address = "http://localhost" // Placeholder, won't be used
            };
        }
        else
        {
            // Single destination for the downstream service
            destinations["destination1"] = new DestinationConfig
            {
                Address = route.DownstreamServiceUrl
            };
        }

        // Configure health checks
        var healthCheckConfig = gatewayOptions.LoadBalancing.EnableHealthChecks
            ? new HealthCheckConfig
            {
                Active = new ActiveHealthCheckConfig
                {
                    Enabled = true,
                    Interval = TimeSpan.FromSeconds(gatewayOptions.LoadBalancing.HealthCheckIntervalSeconds),
                    Timeout = TimeSpan.FromSeconds(5),
                    Policy = "ConsecutiveFailures",
                    Path = "/health/ready"
                }
            }
            : null;

        // Configure load balancing policy
        var loadBalancingPolicy = gatewayOptions.LoadBalancing.Strategy switch
        {
            "RoundRobin" => "RoundRobin",
            "LeastConnections" => "LeastRequests",
            "Weighted" => "Random",
            _ => "RoundRobin"
        };

        return new ClusterConfig
        {
            ClusterId = $"{route.RouteId}-cluster",
            Destinations = destinations,
            HealthCheck = healthCheckConfig,
            LoadBalancingPolicy = loadBalancingPolicy
        };
    }

    /// <summary>
    /// In-memory configuration provider for YARP
    /// </summary>
    private class InMemoryConfigProvider : IProxyConfigProvider
    {
        private readonly InMemoryConfig _config;

        public InMemoryConfigProvider(IReadOnlyList<YarpRouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            _config = new InMemoryConfig(routes, clusters);
        }

        public IProxyConfig GetConfig() => _config;

        private class InMemoryConfig : IProxyConfig
        {
            private readonly CancellationTokenSource _cts = new();

            public InMemoryConfig(IReadOnlyList<YarpRouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
            {
                Routes = routes;
                Clusters = clusters;
                ChangeToken = new CancellationChangeToken(_cts.Token);
            }

            public IReadOnlyList<YarpRouteConfig> Routes { get; }
            public IReadOnlyList<ClusterConfig> Clusters { get; }
            public IChangeToken ChangeToken { get; }
        }
    }
}
