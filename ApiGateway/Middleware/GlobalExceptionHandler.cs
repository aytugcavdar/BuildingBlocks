using System.Text.Json;
using ApiGateway.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace ApiGateway.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
        
        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}", 
            correlationId);
        
        var problemDetails = CreateProblemDetails(exception, httpContext, correlationId);
        
        httpContext.Response.StatusCode = problemDetails.Status;
        httpContext.Response.ContentType = "application/problem+json";
        
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        return true;
    }
    
    private Models.ProblemDetails CreateProblemDetails(
        Exception exception,
        HttpContext context,
        string correlationId)
    {
        return exception switch
        {
            GatewayConfigurationException configEx => new Models.ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Gateway Configuration Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "The gateway is misconfigured. Please contact the administrator.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            DownstreamServiceException downstreamEx => new Models.ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                Title = "Downstream Service Error",
                Status = downstreamEx.StatusCode ?? StatusCodes.Status503ServiceUnavailable,
                Detail = $"The downstream service '{downstreamEx.ServiceName}' is unavailable or returned an error.",
                Instance = context.Request.Path,
                CorrelationId = correlationId,
                Extensions = new Dictionary<string, object>
                {
                    ["serviceName"] = downstreamEx.ServiceName
                }
            },
            
            AggregationException aggEx => new Models.ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Request Aggregation Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Failed to aggregate responses from multiple services.",
                Instance = context.Request.Path,
                CorrelationId = correlationId,
                Extensions = new Dictionary<string, object>
                {
                    ["serviceErrors"] = aggEx.ServiceErrors
                }
            },
            
            TransformationException transEx => new Models.ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Transformation Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = $"Failed to transform {transEx.TransformationType}.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            TimeoutException => new Models.ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5",
                Title = "Gateway Timeout",
                Status = StatusCodes.Status504GatewayTimeout,
                Detail = "The request timed out while waiting for a response from the downstream service.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            _ => new Models.ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            }
        };
    }
}
