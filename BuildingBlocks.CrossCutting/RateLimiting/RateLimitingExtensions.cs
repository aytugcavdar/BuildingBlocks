using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace BuildingBlocks.CrossCutting.RateLimiting;

public static class RateLimitingExtensions
{
    private const string FixedPolicy = "fixed";

    /// <summary>
    /// Adds .NET native rate limiting with fixed window policy
    /// </summary>
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, int permitLimit = 100, int windowTimeInSeconds = 60)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var jsonResponse = $@"{{
                    ""title"": ""Too Many Requests"",
                    ""status"": 429,
                    ""detail"": ""API rate limit exceeded. Please try again later.""
                }}";

                await context.HttpContext.Response.WriteAsync(jsonResponse, cancellationToken: token);
            };

            // Define fixed window limiter policy using AddPolicy
            options.AddPolicy(FixedPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowTimeInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    /// <summary>
    /// Adds rate limiter middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseCustomRateLimiting(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }
}
