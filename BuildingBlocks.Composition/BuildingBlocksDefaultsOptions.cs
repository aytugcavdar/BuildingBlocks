using System.Reflection;

namespace BuildingBlocks.Composition;

public class BuildingBlocksDefaultsOptions
{
    public string? ApplicationName { get; set; }
    public Assembly? ConsumersAssembly { get; set; }

    public bool UseLogging { get; set; } = true;
    public bool UseSecurityServices { get; set; } = true;
    public bool UseJwtAuthentication { get; set; } = false;
    public bool UseCaching { get; set; } = true;
    public bool UseMessaging { get; set; } = true;
    public bool UseHealthChecks { get; set; } = true;
    public bool UseExceptionHandling { get; set; } = true;
    public bool UseRateLimiting { get; set; } = false;
    public bool UseDistributedLocking { get; set; } = false;
    public bool UseSmsServices { get; set; } = false;

    public int RateLimitPermitLimit { get; set; } = 100;
    public int RateLimitWindowSeconds { get; set; } = 60;
}
