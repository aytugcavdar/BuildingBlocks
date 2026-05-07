using BuildingBlocks.HealthChecks.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.HealthChecks.Checks;

/// <summary>
/// Health check for disk space availability.
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly DiskThresholds _thresholds;

    public DiskSpaceHealthCheck(IOptions<HealthCheckOptions> options)
    {
        _thresholds = options.Value.Disk;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var driveInfo = new DriveInfo(_thresholds.MonitoredPath);

            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Drive for path '{_thresholds.MonitoredPath}' is not ready"));
            }

            var availableSpace = driveInfo.AvailableFreeSpace;
            var totalSpace = driveInfo.TotalSize;

            var data = new Dictionary<string, object>
            {
                { "Drive", driveInfo.Name },
                { "TotalSpace", totalSpace },
                { "AvailableSpace", availableSpace },
                { "UsedSpace", totalSpace - availableSpace },
                { "DegradedThreshold", _thresholds.DegradedThresholdBytes },
                { "UnhealthyThreshold", _thresholds.UnhealthyThresholdBytes }
            };

            if (availableSpace < _thresholds.UnhealthyThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Available disk space ({FormatBytes(availableSpace)}) is below unhealthy threshold ({FormatBytes(_thresholds.UnhealthyThresholdBytes)})",
                    data: data));
            }

            if (availableSpace < _thresholds.DegradedThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Available disk space ({FormatBytes(availableSpace)}) is below degraded threshold ({FormatBytes(_thresholds.DegradedThresholdBytes)})",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Available disk space: {FormatBytes(availableSpace)} of {FormatBytes(totalSpace)}",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Error checking disk space for path '{_thresholds.MonitoredPath}': {ex.Message}",
                exception: ex));
        }
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
