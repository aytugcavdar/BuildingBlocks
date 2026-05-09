using ApiGateway.Configuration;
using ApiGateway.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Yarp.ReverseProxy.Configuration;
using GatewayRouteConfig = ApiGateway.Configuration.RouteConfig;

namespace ApiGateway.Tests.Configuration;

public class YarpRouteBuilderTests
{
    private readonly ILogger<YarpRouteBuilder> _logger;

    public YarpRouteBuilderTests()
    {
        _logger = Substitute.For<ILogger<YarpRouteBuilder>>();
    }

    [Fact]
    public void BuildConfiguration_ShouldFilterDisabledRoutes()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "enabled-route",
                    UpstreamPathPattern = "/api/enabled",
                    DownstreamServiceUrl = "http://service1",
                    Enabled = true
                },
                new()
                {
                    RouteId = "disabled-route",
                    UpstreamPathPattern = "/api/disabled",
                    DownstreamServiceUrl = "http://service2",
                    Enabled = false
                }
            }
        });

        // Act
        var builder = new YarpRouteBuilder(configuration, _logger);
        var config = builder.GetConfig();

        // Assert
        config.Routes.Should().HaveCount(1);
        config.Routes.First().RouteId.Should().Be("enabled-route");
        config.Clusters.Should().HaveCount(1);
    }

    [Fact]
    public void BuildYarpRoute_ShouldIncludeApiVersionMetadata()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "versioned-route",
                    UpstreamPathPattern = "/api/v2/users",
                    DownstreamServiceUrl = "http://users-service",
                    ApiVersion = "v2",
                    IsDefaultVersion = true,
                    Enabled = true
                }
            }
        });

        // Act
        var builder = new YarpRouteBuilder(configuration, _logger);
        var config = builder.GetConfig();

        // Assert
        var route = config.Routes.First();
        var metadata = route.Metadata!;
        metadata.Should().ContainKey("ApiVersion");
        metadata["ApiVersion"].Should().Be("v2");
        metadata.Should().ContainKey("IsDefaultVersion");
        metadata["IsDefaultVersion"].Should().Be("True");
    }

    [Fact]
    public void BuildYarpRoute_ShouldIncludeRateLimitMetadata()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "rate-limited-route",
                    UpstreamPathPattern = "/api/limited",
                    DownstreamServiceUrl = "http://service",
                    RateLimit = new RateLimitPolicy
                    {
                        PermitLimit = 100,
                        WindowSeconds = 60,
                        PartitionBy = "user"
                    },
                    Enabled = true
                }
            }
        });

        // Act
        var builder = new YarpRouteBuilder(configuration, _logger);
        var config = builder.GetConfig();

        // Assert
        var route = config.Routes.First();
        var metadata = route.Metadata!;
        metadata.Should().ContainKey("RateLimit.PermitLimit");
        metadata["RateLimit.PermitLimit"].Should().Be("100");
        metadata.Should().ContainKey("RateLimit.WindowSeconds");
        metadata["RateLimit.WindowSeconds"].Should().Be("60");
        metadata.Should().ContainKey("RateLimit.PartitionBy");
        metadata["RateLimit.PartitionBy"].Should().Be("user");
    }

    [Fact]
    public void BuildYarpRoute_ShouldIncludeConfiguredHttpMethods()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "method-route",
                    UpstreamPathPattern = "/api/methods",
                    DownstreamServiceUrl = "http://service",
                    HttpMethods = new List<string> { "GET", "post" },
                    Enabled = true
                }
            }
        });

        // Act
        var builder = new YarpRouteBuilder(configuration, _logger);
        var config = builder.GetConfig();

        // Assert
        var route = config.Routes.First();
        route.Match.Methods.Should().BeEquivalentTo("GET", "POST");
    }

    [Fact]
    public void BuildYarpRoute_ShouldIncludeCacheMetadata()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "cached-route",
                    UpstreamPathPattern = "/api/cached",
                    DownstreamServiceUrl = "http://service",
                    Cache = new CachePolicy
                    {
                        Enabled = true,
                        TtlSeconds = 300,
                        VaryByHeaders = new List<string> { "Accept-Language", "Authorization" },
                        VaryByQueryParams = new List<string> { "page", "size" }
                    },
                    Enabled = true
                }
            }
        });

        // Act
        var builder = new YarpRouteBuilder(configuration, _logger);
        var config = builder.GetConfig();

        // Assert
        var route = config.Routes.First();
        var metadata = route.Metadata!;
        metadata.Should().ContainKey("Cache.Enabled");
        metadata["Cache.Enabled"].Should().Be("true");
        metadata.Should().ContainKey("Cache.TtlSeconds");
        metadata["Cache.TtlSeconds"].Should().Be("300");
        metadata.Should().ContainKey("Cache.VaryByHeaders");
        metadata["Cache.VaryByHeaders"].Should().Be("Accept-Language,Authorization");
        metadata.Should().ContainKey("Cache.VaryByQueryParams");
        metadata["Cache.VaryByQueryParams"].Should().Be("page,size");
    }

    [Fact]
    public void BuildYarpRoute_ShouldIncludeAggregationMetadata()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "aggregation-route",
                    UpstreamPathPattern = "/api/aggregate",
                    DownstreamServiceUrl = "http://aggregator",
                    IsAggregation = true,
                    AggregationTargets = new List<string> { "users-service", "orders-service", "products-service" },
                    Enabled = true
                }
            }
        });

        // Act
        var builder = new YarpRouteBuilder(configuration, _logger);
        var config = builder.GetConfig();

        // Assert
        var route = config.Routes.First();
        var metadata = route.Metadata!;
        metadata.Should().ContainKey("IsAggregation");
        metadata["IsAggregation"].Should().Be("true");
        metadata.Should().ContainKey("AggregationTargets");
        metadata["AggregationTargets"].Should().Be("users-service,orders-service,products-service");
    }

    [Fact]
    public void BuildYarpRoute_ShouldAddPathPatternTransform_WhenDownstreamPathDiffers()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "transformed-route",
                    UpstreamPathPattern = "/api/v1/users/{id}",
                    DownstreamPathTemplate = "/internal/user-service/users/{id}",
                    DownstreamServiceUrl = "http://users-service",
                    Enabled = true
                }
            }
        });

        // Act
        var builder = new YarpRouteBuilder(configuration, _logger);
        var config = builder.GetConfig();

        // Assert
        var route = config.Routes.First();
        route.Transforms.Should().NotBeNull();
        route.Transforms.Should().Contain(t => 
            t.ContainsKey("PathPattern") && 
            t["PathPattern"] == "/internal/user-service/users/{id}");
    }

    [Fact]
    public void BuildYarpRoute_ShouldNotAddPathPatternTransform_WhenPathsAreSame()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "no-transform-route",
                    UpstreamPathPattern = "/api/users/{id}",
                    DownstreamPathTemplate = "/api/users/{id}",
                    DownstreamServiceUrl = "http://users-service",
                    Enabled = true
                }
            }
        });

        // Act
        var builder = new YarpRouteBuilder(configuration, _logger);
        var config = builder.GetConfig();

        // Assert
        var route = config.Routes.First();
        route.Transforms.Should().NotContain(t => t.ContainsKey("PathPattern"));
    }

    [Fact]
    public void BuildConfiguration_ShouldThrowGatewayConfigurationException_WhenRouteIdIsEmpty()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "",
                    UpstreamPathPattern = "/api/test",
                    DownstreamServiceUrl = "http://service",
                    Enabled = true
                }
            }
        });

        // Act & Assert
        var act = () => new YarpRouteBuilder(configuration, _logger);
        act.Should().Throw<GatewayConfigurationException>()
            .WithMessage("*RouteId*");
    }

    [Fact]
    public void BuildConfiguration_ShouldThrowGatewayConfigurationException_WhenUpstreamPathIsEmpty()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "test-route",
                    UpstreamPathPattern = "",
                    DownstreamServiceUrl = "http://service",
                    Enabled = true
                }
            }
        });

        // Act & Assert
        var act = () => new YarpRouteBuilder(configuration, _logger);
        act.Should().Throw<GatewayConfigurationException>();
    }

    [Fact]
    public void BuildConfiguration_ShouldThrowGatewayConfigurationException_WhenDownstreamUrlIsInvalid()
    {
        // Arrange
        var configuration = CreateConfiguration(new GatewayOptions
        {
            Routes = new List<GatewayRouteConfig>
            {
                new()
                {
                    RouteId = "test-route",
                    UpstreamPathPattern = "/api/test",
                    DownstreamServiceUrl = "invalid-url",
                    Enabled = true
                }
            }
        });

        // Act & Assert
        var act = () => new YarpRouteBuilder(configuration, _logger);
        act.Should().Throw<GatewayConfigurationException>();
    }

    private IConfiguration CreateConfiguration(GatewayOptions options)
    {
        var configDict = new Dictionary<string, string?>
        {
            ["Gateway:Routes:0:RouteId"] = options.Routes.FirstOrDefault()?.RouteId,
            ["Gateway:Routes:0:UpstreamPathPattern"] = options.Routes.FirstOrDefault()?.UpstreamPathPattern,
            ["Gateway:Routes:0:DownstreamServiceUrl"] = options.Routes.FirstOrDefault()?.DownstreamServiceUrl,
            ["Gateway:Routes:0:DownstreamPathTemplate"] = options.Routes.FirstOrDefault()?.DownstreamPathTemplate,
            ["Gateway:Routes:0:Enabled"] = options.Routes.FirstOrDefault()?.Enabled.ToString(),
            ["Gateway:Routes:0:ApiVersion"] = options.Routes.FirstOrDefault()?.ApiVersion,
            ["Gateway:Routes:0:IsDefaultVersion"] = options.Routes.FirstOrDefault()?.IsDefaultVersion.ToString(),
            ["Gateway:Routes:0:IsAggregation"] = options.Routes.FirstOrDefault()?.IsAggregation.ToString()
        };

        // Add rate limit config if present
        var firstRoute = options.Routes.FirstOrDefault();
        if (firstRoute?.RateLimit != null)
        {
            configDict["Gateway:Routes:0:RateLimit:PermitLimit"] = firstRoute.RateLimit.PermitLimit.ToString();
            configDict["Gateway:Routes:0:RateLimit:WindowSeconds"] = firstRoute.RateLimit.WindowSeconds.ToString();
            configDict["Gateway:Routes:0:RateLimit:PartitionBy"] = firstRoute.RateLimit.PartitionBy;
        }

        if (firstRoute?.HttpMethods != null)
        {
            for (int i = 0; i < firstRoute.HttpMethods.Count; i++)
            {
                configDict[$"Gateway:Routes:0:HttpMethods:{i}"] = firstRoute.HttpMethods[i];
            }
        }

        // Add cache config if present
        if (firstRoute?.Cache != null)
        {
            configDict["Gateway:Routes:0:Cache:Enabled"] = firstRoute.Cache.Enabled.ToString();
            configDict["Gateway:Routes:0:Cache:TtlSeconds"] = firstRoute.Cache.TtlSeconds.ToString();
            
            for (int i = 0; i < firstRoute.Cache.VaryByHeaders.Count; i++)
            {
                configDict[$"Gateway:Routes:0:Cache:VaryByHeaders:{i}"] = firstRoute.Cache.VaryByHeaders[i];
            }
            
            for (int i = 0; i < firstRoute.Cache.VaryByQueryParams.Count; i++)
            {
                configDict[$"Gateway:Routes:0:Cache:VaryByQueryParams:{i}"] = firstRoute.Cache.VaryByQueryParams[i];
            }
        }

        // Add aggregation targets if present
        if (firstRoute?.AggregationTargets != null)
        {
            for (int i = 0; i < firstRoute.AggregationTargets.Count; i++)
            {
                configDict[$"Gateway:Routes:0:AggregationTargets:{i}"] = firstRoute.AggregationTargets[i];
            }
        }

        // Add second route if exists (for disabled route test)
        if (options.Routes.Count > 1)
        {
            var secondRoute = options.Routes[1];
            configDict["Gateway:Routes:1:RouteId"] = secondRoute.RouteId;
            configDict["Gateway:Routes:1:UpstreamPathPattern"] = secondRoute.UpstreamPathPattern;
            configDict["Gateway:Routes:1:DownstreamServiceUrl"] = secondRoute.DownstreamServiceUrl;
            configDict["Gateway:Routes:1:Enabled"] = secondRoute.Enabled.ToString();
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();
    }
}
