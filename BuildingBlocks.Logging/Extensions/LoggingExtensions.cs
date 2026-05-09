using BuildingBlocks.Logging.Configurations;
using BuildingBlocks.Logging.Enrichers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace BuildingBlocks.Logging.Extensions;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddBuildingBlocksLogging(
        this WebApplicationBuilder builder,
        string applicationName)
    {
        return builder.AddSerilogLogging(applicationName);
    }

    /// <summary>
    /// Serilog'u ASP.NET Core uygulamasına ekler.
    /// Development: Debug seviye + Console + File + Seq
    /// Production: Information seviye + Console + Seq
    /// Kullanım: builder.AddSerilogLogging("Catalog.API");
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(
        this WebApplicationBuilder builder,
        string applicationName)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<CorrelationIdEnricher>();

        // Uygulama başlamadan önce bootstrap logger (startup hatalarını yakalar)
        Log.Logger = SerilogConfiguration.CreateBootstrapLogger(applicationName);
        Log.Information("🚀 Starting {ApplicationName}...", applicationName);

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            var seqUrl = context.Configuration["Serilog:WriteTo:2:Args:serverUrl"]
                      ?? context.Configuration["Serilog:WriteTo:1:Args:serverUrl"]
                      ?? "http://seq:5341";

            var correlationEnricher = services.GetRequiredService<CorrelationIdEnricher>();

            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", applicationName)
                    .Enrich.WithProperty("Environment", "Development")
                    .Enrich.With(correlationEnricher)
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                    .WriteTo.File(
                        path: $"Logs/{applicationName}-dev-.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7)
                    .WriteTo.Seq(seqUrl);
            }
            else
            {
                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithProperty("Application", applicationName)
                    .Enrich.WithProperty("Environment", "Production")
                    .Enrich.With(correlationEnricher)
                    .WriteTo.Console()
                    .WriteTo.File(
                        path: $"Logs/{applicationName}-.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        fileSizeLimitBytes: 100_000_000,
                        rollOnFileSizeLimit: true)
                    .WriteTo.Seq(seqUrl);
            }
        });

        Log.Information("✅ Serilog configured successfully");
        return builder;
    }

    /// <summary>
    /// Serilog request logging middleware'i ekler.
    /// Her HTTP isteğini otomatik loglar (method, path, status code, elapsed ms).
    /// </summary>
    public static WebApplication UseSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

                if (httpContext.Request.QueryString.HasValue)
                    diagnosticContext.Set("QueryString", httpContext.Request.QueryString.Value ?? string.Empty);

                if (httpContext.Response.ContentLength.HasValue)
                    diagnosticContext.Set("ResponseSize", httpContext.Response.ContentLength.Value);

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value
                              ?? httpContext.User.FindFirst("nameidentifier")?.Value;
                    if (!string.IsNullOrEmpty(userId))
                        diagnosticContext.Set("UserId", userId);
                }
            };

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null || httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;
                if (elapsed > 1000)
                    return LogEventLevel.Warning;
                return LogEventLevel.Information;
            };
        });

        return app;
    }

    public static WebApplication UseBuildingBlocksRequestLogging(this WebApplication app)
    {
        return app.UseSerilogRequestLogging();
    }
}
