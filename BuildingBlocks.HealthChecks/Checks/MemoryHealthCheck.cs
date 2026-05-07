using BuildingBlocks.HealthChecks.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.HealthChecks.Checks;

/// <summary>
/// Health check for system memory availability.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly MemoryThresholds _thresholds;

    public MemoryHealthCheck(IOptions<HealthCheckOptions> options)
    {
        _thresholds = options.Value.Memory;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var availableMemory = gcMemoryInfo.TotalAvailableMemoryBytes - gcMemoryInfo.MemoryLoadBytes;

        var data = new Dictionary<string, object>
        {
            { "TotalAvailableMemory", gcMemoryInfo.TotalAvailableMemoryBytes },
            { "MemoryLoad", gcMemoryInfo.MemoryLoadBytes },
            { "AvailableMemory", availableMemory },
            { "DegradedThreshold", _thresholds.DegradedThresholdBytes },
            { "UnhealthyThreshold", _thresholds.UnhealthyThresholdBytes }
        };

        if (availableMemory < _thresholds.UnhealthyThresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Available memory ({FormatBytes(availableMemory)}) is below unhealthy threshold ({FormatBytes(_thresholds.UnhealthyThresholdBytes)})",
                data: data));
        }

        if (availableMemory < _thresholds.DegradedThresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Available memory ({FormatBytes(availableMemory)}) is below degraded threshold ({FormatBytes(_thresholds.DegradedThresholdBytes)})",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Available memory: {FormatBytes(availableMemory)}",
            data: data));
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
