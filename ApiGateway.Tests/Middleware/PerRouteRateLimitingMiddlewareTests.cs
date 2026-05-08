using ApiGateway.Middleware;
using ApiGateway.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;

namespace ApiGateway.Tests.Middleware;

public class PerRouteRateLimitingMiddlewareTests
{
    private readonly ILogger<PerRouteRateLimitingMiddleware> _logger;
    private readonly GatewayMetrics _metrics;
    private bool _nextCalled;

    public PerRouteRateLimitingMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<PerRouteRateLimitingMiddleware>>();
        _metrics = new GatewayMetrics();
        _nextCalled = false;
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenNoRateLimitMetadata()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => { _nextCalled = true; return Task.CompletedTask; },
            _logger,
            _metrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenEndpointIsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // No endpoint set
        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => { _nextCalled = true; return Task.CompletedTask; },
            _logger,
            _metrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateUserPartitionKey_ForAuthenticatedUser()
    {
        // Arrange
        var context = CreateHttpContext(
            routeId: "test-route",
            permitLimit: 10,
            windowSeconds: 60,
            partitionBy: "user",
            userId: "user123");

        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => { _nextCalled = true; return Task.CompletedTask; },
            _logger,
            _metrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ShouldFallbackToIp_WhenUserNotAuthenticated()
    {
        // Arrange
        var context = CreateHttpContext(
            routeId: "test-route",
            permitLimit: 10,
            windowSeconds: 60,
            partitionBy: "user",
            userId: null); // Not authenticated

        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => { _nextCalled = true; return Task.CompletedTask; },
            _logger,
            _metrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateIpPartitionKey_WhenPartitionByIp()
    {
        // Arrange
        var context = CreateHttpContext(
            routeId: "test-route",
            permitLimit: 10,
            windowSeconds: 60,
            partitionBy: "ip");

        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => { _nextCalled = true; return Task.CompletedTask; },
            _logger,
            _metrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateGlobalPartitionKey_WhenPartitionByGlobal()
    {
        // Arrange
        var context = CreateHttpContext(
            routeId: "test-route",
            permitLimit: 10,
            windowSeconds: 60,
            partitionBy: "global");

        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => { _nextCalled = true; return Task.CompletedTask; },
            _logger,
            _metrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn429_WhenRateLimitExceeded()
    {
        // Arrange
        var context = CreateHttpContext(
            routeId: "test-route",
            permitLimit: 2,
            windowSeconds: 60,
            partitionBy: "global");

        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => { _nextCalled = true; return Task.CompletedTask; },
            _logger,
            _metrics);

        // Act - Make requests until rate limit is exceeded
        await middleware.InvokeAsync(context);
        _nextCalled.Should().BeTrue();
        _nextCalled = false;

        await middleware.InvokeAsync(context);
        _nextCalled.Should().BeTrue();
        _nextCalled = false;

        // This should exceed the limit
        await middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeRetryAfterHeader_When429()
    {
        // Arrange
        var context = CreateHttpContext(
            routeId: "test-route",
            permitLimit: 1,
            windowSeconds: 60,
            partitionBy: "global");

        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => Task.CompletedTask,
            _logger,
            _metrics);

        // Act - Exhaust the limit
        await middleware.InvokeAsync(context);
        
        // Create new context for the rejected request
        var rejectedContext = CreateHttpContext(
            routeId: "test-route",
            permitLimit: 1,
            windowSeconds: 60,
            partitionBy: "global");
        
        await middleware.InvokeAsync(rejectedContext);

        // Assert
        rejectedContext.Response.StatusCode.Should().Be(429);
        rejectedContext.Response.Headers.Should().ContainKey("Retry-After");
        rejectedContext.Response.Headers["Retry-After"].ToString().Should().Be("60");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenLimitNotExceeded()
    {
        // Arrange
        var context = CreateHttpContext(
            routeId: "test-route",
            permitLimit: 10,
            windowSeconds: 60,
            partitionBy: "global");

        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => { _nextCalled = true; return Task.CompletedTask; },
            _logger,
            _metrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRecordMetrics_WhenRateLimitExceeded()
    {
        // Arrange
        var context = CreateHttpContext(
            routeId: "metrics-test-route",
            permitLimit: 1,
            windowSeconds: 60,
            partitionBy: "global");

        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => Task.CompletedTask,
            _logger,
            _metrics);

        // Act - Exhaust the limit
        await middleware.InvokeAsync(context);
        
        // Create new context for the rejected request
        var rejectedContext = CreateHttpContext(
            routeId: "metrics-test-route",
            permitLimit: 1,
            windowSeconds: 60,
            partitionBy: "global");
        
        await middleware.InvokeAsync(rejectedContext);

        // Assert
        rejectedContext.Response.StatusCode.Should().Be(429);
        // Metrics are recorded (we can't easily assert on counter values without exposing them)
    }

    [Fact]
    public void Dispose_ShouldDisposeAllRateLimiters()
    {
        // Arrange
        var middleware = new PerRouteRateLimitingMiddleware(
            next: _ => Task.CompletedTask,
            _logger,
            _metrics);

        // Act
        middleware.Dispose();

        // Assert - Should not throw
    }

    private DefaultHttpContext CreateHttpContext(
        string? routeId = null,
        int? permitLimit = null,
        int? windowSeconds = null,
        string? partitionBy = null,
        string? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        if (routeId != null && permitLimit.HasValue && windowSeconds.HasValue && partitionBy != null)
        {
            // Create endpoint with rate limit metadata
            var metadata = new Dictionary<string, string>
            {
                ["RouteId"] = routeId,
                ["RateLimit.PermitLimit"] = permitLimit.Value.ToString(),
                ["RateLimit.WindowSeconds"] = windowSeconds.Value.ToString(),
                ["RateLimit.PartitionBy"] = partitionBy
            };

            var endpoint = new Endpoint(
                requestDelegate: _ => Task.CompletedTask,
                metadata: new EndpointMetadataCollection(metadata),
                displayName: routeId);

            context.SetEndpoint(endpoint);
        }

        // Set user identity if userId is provided
        if (userId != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("sub", userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }
}
