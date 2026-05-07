using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Logging.Extensions;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Servisler için standart OpenTelemetry(Tracing) yapılandırmasını ekler.
    /// Kayıt: AddAspNetCoreInstrumentation, AddHttpClientInstrumentation, MassTransit.
    /// </summary>
    public static IServiceCollection AddCustomOpenTelemetry(
        this IServiceCollection services, 
        string serviceName, 
        string serviceVersion = "1.0.0")
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, httpRequest) =>
                    {
                        activity.SetTag("http.request.size", httpRequest.ContentLength);
                    };
                    options.EnrichWithHttpResponse = (activity, httpResponse) =>
                    {
                        activity.SetTag("http.response.size", httpResponse.ContentLength);
                    };
                    options.Filter = (httpContext) =>
                    {
                        return !httpContext.Request.Path.StartsWithSegments("/health");
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddSource("MassTransit")
                .AddConsoleExporter() 
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:4317");
                })
            );

        return services;
    }
}
