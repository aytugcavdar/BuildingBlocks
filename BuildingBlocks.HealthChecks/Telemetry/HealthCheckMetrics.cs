using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.HealthChecks.Telemetry;

/// <summary>
/// OpenTelemetry metrics integration for health checks.
/// Records execution count, duration, and status for all health checks.
/// </summary>
public class HealthCheckMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _executionCounter;
    private readonly Histogram<double> _durationHistogram;
    private readonly ObservableGauge<int> _statusGauge;
    private readonly ConcurrentDictionary<string, HealthStatus> _currentStatuses = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckMetrics"/> class.
    /// </summary>
    public HealthCheckMetrics()
    {
        _meter = new Meter("BuildingBlocks.HealthChecks", "1.0.0");

        _executionCounter = _meter.CreateCounter<long>(
            "healthcheck.executions",
            description: "Total number of health check executions");

        _durationHistogram = _meter.CreateHistogram<double>(
            "healthcheck.duration",
            unit: "ms",
            description: "Health check execution duration");

        _statusGauge = _meter.CreateObservableGauge<int>(
            "healthcheck.status",
            () => _currentStatuses.Select(kvp => new Measurement<int>(
                (int)kvp.Value,
                new KeyValuePair<string, object?>("check_name", kvp.Key))),
            description: "Current health check status (0=Unhealthy, 1=Degraded, 2=Healthy)");
    }

    /// <summary>
    /// Records a health check execution with duration and status.
    /// </summary>
    /// <param name="checkName">The name of the health check.</param>
    /// <param name="status">The health status result.</param>
    /// <param name="durationMs">The execution duration in milliseconds.</param>
    public void RecordExecution(string checkName, HealthStatus status, double durationMs)
    {
        _executionCounter.Add(1,
            new KeyValuePair<string, object?>("check_name", checkName),
            new KeyValuePair<string, object?>("status", status.ToString()));

        _durationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("check_name", checkName),
            new KeyValuePair<string, object?>("status", status.ToString()));

        _currentStatuses[checkName] = status;
    }
}
