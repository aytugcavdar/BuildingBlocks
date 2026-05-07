using ApiGateway.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ApiGateway")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add API Gateway services
builder.Services.AddApiGateway(builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
app.UseApiGateway();

try
{
    Log.Information("Starting API Gateway");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
