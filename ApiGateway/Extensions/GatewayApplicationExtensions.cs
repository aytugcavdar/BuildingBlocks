using ApiGateway.Configuration;
using ApiGateway.Helpers;
using ApiGateway.Middleware;
using Microsoft.Extensions.Options;

namespace ApiGateway.Extensions;

public static class GatewayApplicationExtensions
{
    public static WebApplication UseApiGateway(this WebApplication app)
    {
        var gatewayOptions = app.Services.GetRequiredService<IOptions<GatewayOptions>>().Value;
        // Exception handling (must be first)
        app.UseExceptionHandler();
        
        // CORS (before authentication)
        app.UseCors();
        
        // Security headers
        app.UseMiddleware<SecurityHeadersMiddleware>();
        
        // Metrics middleware (early in pipeline to capture all requests)
        app.UseMiddleware<MetricsMiddleware>();
        
        // Request size limit
        app.Use(async (context, next) =>
        {
            context.Request.Body = new LimitedStream(
                context.Request.Body,
                10485760); // 10MB default
            await next();
        });
        
        // Authentication and Authorization (only if enabled)
        if (gatewayOptions.Authentication.Enabled)
        {
            app.UseAuthentication();
        }
        app.UseAuthorization();

        // Enforce route-level authentication, roles, and policy metadata from Gateway routes.
        app.UseMiddleware<RouteAuthorizationMiddleware>();
        
        // Per-route rate limiting
        app.UseMiddleware<PerRouteRateLimitingMiddleware>();
        
        // Gateway cache
        app.UseMiddleware<GatewayCacheMiddleware>();
        
        // Prometheus metrics endpoint
        app.MapPrometheusScrapingEndpoint();
        
        // Health check endpoints
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Name == "gateway"
        });
        
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true
        });
        
        app.MapHealthChecks("/health/downstream", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Name == "downstream"
        });
        
        // Swagger
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
            options.RoutePrefix = "swagger";
        });
        
        // YARP reverse proxy (must be last)
        app.MapReverseProxy();
        
        return app;
    }
}
