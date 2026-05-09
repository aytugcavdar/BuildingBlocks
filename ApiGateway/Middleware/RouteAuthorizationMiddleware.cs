using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Middleware;

public class RouteAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RouteAuthorizationMiddleware> _logger;
    private readonly IAuthorizationService _authorizationService;

    public RouteAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<RouteAuthorizationMiddleware> logger,
        IAuthorizationService authorizationService)
    {
        _next = next;
        _logger = logger;
        _authorizationService = authorizationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var metadata = context.GetEndpoint()?.Metadata.GetMetadata<IReadOnlyDictionary<string, string>>();
        if (metadata == null)
        {
            await _next(context);
            return;
        }

        var routeId = metadata.TryGetValue("RouteId", out var routeIdValue) ? routeIdValue : "unknown";
        var requiresAuthentication = metadata.TryGetValue("RequireAuthentication", out var requireAuthValue) &&
            bool.TryParse(requireAuthValue, out var parsedRequireAuth) &&
            parsedRequireAuth;

        if (!requiresAuthentication)
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Unauthenticated request rejected for route {RouteId}", routeId);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required.");
            return;
        }

        if (metadata.TryGetValue("RequiredRoles", out var rolesValue) &&
            !HasAnyRole(context.User, rolesValue))
        {
            _logger.LogWarning("Forbidden request rejected for route {RouteId}; missing required role", routeId);
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Forbidden", "A required role is missing.");
            return;
        }

        if (metadata.TryGetValue("RequiredPolicies", out var policiesValue) &&
            !await AuthorizePoliciesAsync(context, policiesValue))
        {
            _logger.LogWarning("Forbidden request rejected for route {RouteId}; missing required policy", routeId);
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Forbidden", "A required policy is missing.");
            return;
        }

        await _next(context);
    }

    private static bool HasAnyRole(ClaimsPrincipal user, string rolesValue)
    {
        var roles = SplitMetadataValue(rolesValue);
        return roles.Length == 0 || roles.Any(user.IsInRole);
    }

    private async Task<bool> AuthorizePoliciesAsync(HttpContext context, string policiesValue)
    {
        var policies = SplitMetadataValue(policiesValue);
        foreach (var policy in policies)
        {
            try
            {
                var result = await _authorizationService.AuthorizeAsync(context.User, context, policy);
                if (!result.Succeeded)
                {
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Authorization policy {PolicyName} is not registered", policy);
                return false;
            }
        }

        return true;
    }

    private static string[] SplitMetadataValue(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
