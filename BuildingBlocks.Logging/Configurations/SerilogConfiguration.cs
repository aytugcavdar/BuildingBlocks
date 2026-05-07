using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Logging.Configurations;

/// <summary>
/// Önceden tanımlanmış Serilog konfigürasyon şablonları.
/// LoggingExtensions.AddSerilogLogging() tarafından kullanılır.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Uygulama başlamadan önce bootstrap logger oluşturur.
    /// DI hazır olmadan önce startup hatalarını yakalamak için kullanılır.
    /// </summary>
    public static ILogger CreateBootstrapLogger(string applicationName)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateBootstrapLogger();
    }

    /// <summary>
    /// Development logger: Debug seviye, Console + File.
    /// LoggingExtensions içinden doğrudan kullanılabilir (standalone ihtiyaç için).
    /// </summary>
    public static ILogger CreateDevelopmentLogger(string applicationName)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithProperty("Environment", "Development")
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: $"Logs/{applicationName}-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Production logger: Information seviye, Console + File + SEQ.
    /// LoggingExtensions içinden doğrudan kullanılabilir (standalone ihtiyaç için).
    /// </summary>
    public static ILogger CreateProductionLogger(IConfiguration configuration, string applicationName)
    {
        var seqUrl = configuration["Serilog:WriteTo:1:Args:serverUrl"] ?? "http://localhost:5341";

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithProperty("Environment", "Production")
            .Enrich.WithMachineName()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: $"Logs/{applicationName}-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 100_000_000,
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        if (!string.IsNullOrEmpty(seqUrl))
            loggerConfig.WriteTo.Seq(seqUrl);

        return loggerConfig.CreateLogger();
    }
}
