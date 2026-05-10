using BuildingBlocks.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var features = builder.Configuration
    .GetSection("Features")
    .Get<FeatureOptions>() ?? new FeatureOptions();

builder.AddBuildingBlocksDefaults(options =>
{
    options.ApplicationName = builder.Configuration["Application:Name"]
        ?? builder.Environment.ApplicationName;
    options.ConsumersAssembly = typeof(Program).Assembly;

    options.UseCaching = features.Caching;
    options.UseMessaging = features.Messaging;
    options.UseHealthChecks = features.HealthChecks;
    options.UseJwtAuthentication = features.JwtAuthentication;
    options.UseRateLimiting = features.RateLimiting;
    options.UseDistributedLocking = features.DistributedLocking;
    options.UseSmsServices = features.Sms;
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseBuildingBlocksDefaults(options =>
{
    options.UseHealthChecks = features.HealthChecks;
    options.UseJwtAuthentication = features.JwtAuthentication;
    options.UseRateLimiting = features.RateLimiting;
});

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = builder.Configuration["Application:Name"] ?? app.Environment.ApplicationName,
    environment = app.Environment.EnvironmentName,
    status = "running",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();

public sealed class FeatureOptions
{
    public bool Caching { get; set; } = true;
    public bool Messaging { get; set; } = false;
    public bool HealthChecks { get; set; } = true;
    public bool JwtAuthentication { get; set; } = false;
    public bool RateLimiting { get; set; } = false;
    public bool DistributedLocking { get; set; } = false;
    public bool Sms { get; set; } = false;
}
