using System.Security.Claims;
using ApiGateway.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApiGateway.Tests.Middleware;

public class RouteAuthorizationMiddlewareTests
{
    private readonly ILogger<RouteAuthorizationMiddleware> _logger;
    private readonly IAuthorizationService _authorizationService;
    private bool _nextCalled;

    public RouteAuthorizationMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<RouteAuthorizationMiddleware>>();
        _authorizationService = Substitute.For<IAuthorizationService>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_WhenAuthenticationIsRequiredAndUserIsAnonymous()
    {
        var context = CreateContext(requireAuthentication: true);
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenAuthenticationIsRequiredAndUserIsAuthenticated()
    {
        var context = CreateContext(requireAuthentication: true, claims: new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") });
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn403_WhenRequiredRoleIsMissing()
    {
        var context = CreateContext(
            requireAuthentication: true,
            requiredRoles: "Admin",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") });
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenRequiredRoleIsPresent()
    {
        var context = CreateContext(
            requireAuthentication: true,
            requiredRoles: "Admin",
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Role, "Admin")
            });
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn403_WhenRequiredPolicyFails()
    {
        var context = CreateContext(
            requireAuthentication: true,
            requiredPolicies: "AdminPolicy",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") });
        _authorizationService.AuthorizeAsync(context.User, context, "AdminPolicy")
            .Returns(AuthorizationResult.Failed());
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenRequiredPolicySucceeds()
    {
        var context = CreateContext(
            requireAuthentication: true,
            requiredPolicies: "AdminPolicy",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") });
        _authorizationService.AuthorizeAsync(context.User, context, "AdminPolicy")
            .Returns(AuthorizationResult.Success());
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenRouteDoesNotRequireAuthentication()
    {
        var context = CreateContext(requireAuthentication: false);
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    private RouteAuthorizationMiddleware CreateMiddleware()
    {
        _nextCalled = false;
        return new RouteAuthorizationMiddleware(
            _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            },
            _logger,
            _authorizationService);
    }

    private static DefaultHttpContext CreateContext(
        bool requireAuthentication,
        string? requiredRoles = null,
        string? requiredPolicies = null,
        IEnumerable<Claim>? claims = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        var metadata = new Dictionary<string, string>
        {
            ["RouteId"] = "test-route",
            ["RequireAuthentication"] = requireAuthentication.ToString()
        };

        if (!string.IsNullOrWhiteSpace(requiredRoles))
        {
            metadata["RequiredRoles"] = requiredRoles;
        }

        if (!string.IsNullOrWhiteSpace(requiredPolicies))
        {
            metadata["RequiredPolicies"] = requiredPolicies;
        }

        var endpoint = new Endpoint(
            requestDelegate: _ => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(metadata),
            displayName: "test-route");

        context.SetEndpoint(endpoint);

        if (claims != null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        return context;
    }
}
