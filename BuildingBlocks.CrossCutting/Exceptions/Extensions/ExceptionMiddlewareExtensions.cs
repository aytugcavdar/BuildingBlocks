using BuildingBlocks.CrossCutting.Exceptions.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.CrossCutting.Exceptions.Extensions;

public static class ExceptionMiddlewareExtensions
{
    /// <summary>
    /// Global exception middleware'i pipeline'a ekler.
    /// Program.cs'de en başa konulmalıdır.
    /// </summary>
    public static IApplicationBuilder UseCustomExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseBuildingBlocksExceptionHandling();
    }

    public static IApplicationBuilder UseBuildingBlocksExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }

    /// <summary>
    /// HttpExceptionHandler'ı DI container'a register eder.
    /// AddCustomExceptionMiddleware() çağrısı yeterliydi ama DI için gerekli.
    /// </summary>
    public static IServiceCollection AddCustomExceptionMiddleware(this IServiceCollection services)
    {
        return services.AddBuildingBlocksExceptionHandling();
    }

    public static IServiceCollection AddBuildingBlocksExceptionHandling(this IServiceCollection services)
    {
        services.AddTransient<HttpExceptionHandler>();
        return services;
    }
}
