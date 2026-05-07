using BuildingBlocks.CrossCutting.Exceptions.Handlers;
using BuildingBlocks.CrossCutting.Exceptions.Types;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace BuildingBlocks.CrossCutting.Exceptions;

/// <summary>
/// Global exception handling middleware.
/// Tüm exception'ları yakalar, loglar ve HTTP response'a dönüştürür.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpExceptionHandler _httpExceptionHandler;

    /// <summary>
    /// DI üzerinden HttpExceptionHandler inject edilir.
    /// </summary>
    public ExceptionMiddleware(RequestDelegate next, HttpExceptionHandler httpExceptionHandler)
    {
        _next = next;
        _httpExceptionHandler = httpExceptionHandler;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            using (LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].ToString()))
            using (LogContext.PushProperty("RemoteIP", context.Connection.RemoteIpAddress?.ToString()))
            {
                var userId = context.User?.FindFirst("sub")?.Value
                          ?? context.User?.FindFirst("nameidentifier")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    using (LogContext.PushProperty("UserId", userId))
                        LogException(exception);
                }
                else
                {
                    LogException(exception);
                }
            }

            _httpExceptionHandler.Response = context.Response;
            await _httpExceptionHandler.HandleExceptionAsync(exception);
        }
    }

    private static void LogException(Exception exception)
    {
        switch (exception)
        {
            case BusinessException businessException:
                Log.Warning(businessException,
                    "Business rule violation: {Message}", businessException.Message);
                break;

            case BusinessValidationException validationException:
                Log.Warning(validationException,
                    "Validation failed: {Errors}", string.Join(", ", validationException.Errors));
                break;

            case NotFoundException notFoundException:
                Log.Warning(notFoundException,
                    "Resource not found: {Message}", notFoundException.Message);
                break;

            default:
                Log.Error(exception,
                    "Unhandled exception: {ExceptionType} - {Message}",
                    exception.GetType().Name, exception.Message);
                break;
        }
    }
}
