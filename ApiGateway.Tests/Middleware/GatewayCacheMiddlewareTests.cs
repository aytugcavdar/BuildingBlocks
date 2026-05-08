using ApiGateway.Middleware;
using ApiGateway.Models;
using ApiGateway.Observability;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using BuildingBlocks.CrossCutting.Caching.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Security.Claims;
using System.Text;
using ApiGateway.Configuration;

namespace ApiGateway.Tests.Middleware;

public class GatewayCacheMiddlewareTests
{
    private readonly IDistributedCacheService _cacheService;
    private readonly ILogger<GatewayCacheMiddleware> _logger;
    private readonly GatewayMetrics _metrics;
    private readonly IOptions<GatewayOptions> _options;

    public GatewayCacheMiddlewareTests()
    {
        _cacheService = Substitute.For<IDistributedCacheService>();
        _logger = Substitute.For<ILogger<GatewayCacheMiddleware>>();
        _metrics = new GatewayMetrics();
        _options = Options.Create(new GatewayOptions());
    }

    [Fact]
    public async Task InvokeAsync_ShouldBypassCache_ForNonGetRequests()
    {
        // Arrange
        var context = CreateHttpContext(method: "POST", cacheEnabled: true);
        var nextCalled = false;

        var middleware = new GatewayCacheMiddleware(
            next: _ => { nextCalled = true; return Task.CompletedTask; },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        await _cacheService.DidNotReceive().GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldBypassCache_WhenCacheNotEnabled()
    {
        // Arrange
        var context = CreateHttpContext(method: "GET", cacheEnabled: false);
        var nextCalled = false;

        var middleware = new GatewayCacheMiddleware(
            next: _ => { nextCalled = true; return Task.CompletedTask; },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        await _cacheService.DidNotReceive().GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnCachedResponse_OnCacheHit()
    {
        // Arrange
        var cachedResponse = new CachedResponse
        {
            StatusCode = 200,
            ContentType = "application/json",
            Content = "{\"message\":\"cached\"}",
            Headers = new Dictionary<string, string> { { "X-Custom", "value" } }
        };

        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedResponse);

        var context = CreateHttpContext(method: "GET", cacheEnabled: true);
        var nextCalled = false;

        var middleware = new GatewayCacheMiddleware(
            next: _ => { nextCalled = true; return Task.CompletedTask; },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse(); // Should not call downstream
        context.Response.StatusCode.Should().Be(200);
        context.Response.Headers.Should().ContainKey("X-Cache-Status");
        context.Response.Headers["X-Cache-Status"].ToString().Should().Be("HIT");
        
        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Be("{\"message\":\"cached\"}");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetCacheMissHeader_OnCacheMiss()
    {
        // Arrange
        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedResponse?)null);

        var context = CreateHttpContext(method: "GET", cacheEnabled: true);

        var middleware = new GatewayCacheMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("fresh response");
            },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Cache-Status");
        context.Response.Headers["X-Cache-Status"].ToString().Should().Be("MISS");
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotCallDownstream_OnCacheHit()
    {
        // Arrange
        var cachedResponse = new CachedResponse
        {
            StatusCode = 200,
            ContentType = "application/json",
            Content = "{\"cached\":true}"
        };

        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedResponse);

        var context = CreateHttpContext(method: "GET", cacheEnabled: true);
        var downstreamCalled = false;

        var middleware = new GatewayCacheMiddleware(
            next: _ => { downstreamCalled = true; return Task.CompletedTask; },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        downstreamCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCacheSuccessfulResponses()
    {
        // Arrange
        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedResponse?)null);

        var context = CreateHttpContext(method: "GET", cacheEnabled: true, ttlSeconds: 300);

        var middleware = new GatewayCacheMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"success\":true}");
            },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _cacheService.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Is<CachedResponse>(r => r.StatusCode == 200 && r.Content == "{\"success\":true}"),
            Arg.Is<CacheEntryOptions>(o => o.AbsoluteExpiration != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotCache4xxResponses()
    {
        // Arrange
        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedResponse?)null);

        var context = CreateHttpContext(method: "GET", cacheEnabled: true);

        var middleware = new GatewayCacheMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync("Not Found");
            },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _cacheService.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<CachedResponse>(),
            Arg.Any<CacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotCache5xxResponses()
    {
        // Arrange
        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedResponse?)null);

        var context = CreateHttpContext(method: "GET", cacheEnabled: true);

        var middleware = new GatewayCacheMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync("Internal Server Error");
            },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _cacheService.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<CachedResponse>(),
            Arg.Any<CacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldVaryByQueryParams()
    {
        // Arrange
        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedResponse?)null);

        var context = CreateHttpContext(
            method: "GET",
            cacheEnabled: true,
            varyByQueryParams: "page,size",
            queryString: "?page=1&size=10");

        var capturedKey = string.Empty;
        _cacheService.When(x => x.SetAsync(
                Arg.Any<string>(),
                Arg.Any<CachedResponse>(),
                Arg.Any<CacheEntryOptions>(),
                Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedKey = callInfo.ArgAt<string>(0));

        var middleware = new GatewayCacheMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("response");
            },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedKey.Should().Contain("page=1");
        capturedKey.Should().Contain("size=10");
    }

    [Fact]
    public async Task InvokeAsync_ShouldVaryByHeaders()
    {
        // Arrange
        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedResponse?)null);

        var context = CreateHttpContext(
            method: "GET",
            cacheEnabled: true,
            varyByHeaders: "Accept-Language,Authorization");
        
        context.Request.Headers["Accept-Language"] = "en-US";
        context.Request.Headers["Authorization"] = "Bearer token123";

        var capturedKey = string.Empty;
        _cacheService.When(x => x.SetAsync(
                Arg.Any<string>(),
                Arg.Any<CachedResponse>(),
                Arg.Any<CacheEntryOptions>(),
                Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedKey = callInfo.ArgAt<string>(0));

        var middleware = new GatewayCacheMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("response");
            },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedKey.Should().Contain("Accept-Language=en-US");
        capturedKey.Should().Contain("Authorization=Bearer token123");
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeUserIdInCacheKey_ForAuthenticatedUser()
    {
        // Arrange
        _cacheService.GetAsync<CachedResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedResponse?)null);

        var context = CreateHttpContext(method: "GET", cacheEnabled: true, userId: "user123");

        var capturedKey = string.Empty;
        _cacheService.When(x => x.SetAsync(
                Arg.Any<string>(),
                Arg.Any<CachedResponse>(),
                Arg.Any<CacheEntryOptions>(),
                Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedKey = callInfo.ArgAt<string>(0));

        var middleware = new GatewayCacheMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("response");
            },
            _logger,
            _options,
            _metrics,
            _cacheService);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedKey.Should().Contain("user=user123");
    }

    private DefaultHttpContext CreateHttpContext(
        string method = "GET",
        bool cacheEnabled = false,
        int ttlSeconds = 60,
        string? varyByHeaders = null,
        string? varyByQueryParams = null,
        string? queryString = null,
        string? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = "/api/test";
        
        if (!string.IsNullOrEmpty(queryString))
        {
            context.Request.QueryString = new QueryString(queryString);
        }

        context.Response.Body = new MemoryStream();

        if (cacheEnabled)
        {
            var metadata = new Dictionary<string, string>
            {
                ["RouteId"] = "test-route",
                ["Cache.Enabled"] = "true",
                ["Cache.TtlSeconds"] = ttlSeconds.ToString()
            };

            if (!string.IsNullOrEmpty(varyByHeaders))
            {
                metadata["Cache.VaryByHeaders"] = varyByHeaders;
            }

            if (!string.IsNullOrEmpty(varyByQueryParams))
            {
                metadata["Cache.VaryByQueryParams"] = varyByQueryParams;
            }

            var endpoint = new Endpoint(
                requestDelegate: _ => Task.CompletedTask,
                metadata: new EndpointMetadataCollection(metadata),
                displayName: "test-route");

            context.SetEndpoint(endpoint);
        }

        // Set user identity if userId is provided
        if (userId != null)
        {
            var claims = new List<Claim>
            {
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }

    private async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
