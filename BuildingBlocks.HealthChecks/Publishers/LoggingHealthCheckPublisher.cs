using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.HealthChecks.Publishers;

/// <summary>
/// Health check publisher that logs health state transitions via Serilog.
/// Tracks previous states and logs only when status changes.
/// </summary>
public class LoggingHealthCheckPublisher : IHealthCheckPublisher
{
    private readonly ILogger<LoggingHealthCheckPublisher> _logger;
    private readonly ConcurrentDictionary<string, HealthStatus> _previousStates = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingHealthCheckPublisher"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LoggingHealthCheckPublisher(ILogger<LoggingHealthCheckPublisher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Publishes health check results, logging state transitions.
    /// </summary>
    /// <param name="report">The health report containing all check results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var entry in report.Entries)
        {
            var currentStatus = entry.Value.Status;
            var previousStatus = _previousStates.GetOrAdd(entry.Key, currentStatus);

            if (currentStatus != previousStatus)
            {
                LogStateTransition(entry.Key, previousStatus, currentStatus, entry.Value);
                _previousStates[entry.Key] = currentStatus;
            }
        }

        return Task.CompletedTask;
    }

    private void LogStateTransition(
        string checkName,
        HealthStatus previousStatus,
        HealthStatus currentStatus,
        HealthReportEntry entry)
    {
        var duration = entry.Duration.TotalMilliseconds;
        var description = entry.Description ?? "No description";

        switch (currentStatus)
        {
            case HealthStatus.Unhealthy:
                _logger.LogError(
                    "Health check '{CheckName}' transitioned from {PreviousStatus} to {CurrentStatus}. " +
                    "Duration: {Duration}ms. Description: {Description}. Exception: {Exception}",
                    checkName, previousStatus, currentStatus, duration, description, entry.Exception?.Message);
                break;

            case HealthStatus.Degraded:
                _logger.LogWarning(
                    "Health check '{CheckName}' transitioned from {PreviousStatus} to {CurrentStatus}. " +
                    "Duration: {Duration}ms. Description: {Description}",
                    checkName, previousStatus, currentStatus, duration, description);
                break;

            case HealthStatus.Healthy when previousStatus == HealthStatus.Unhealthy:
                _logger.LogInformation(
                    "Health check '{CheckName}' recovered from {PreviousStatus} to {CurrentStatus}. " +
                    "Duration: {Duration}ms. Description: {Description}",
                    checkName, previousStatus, currentStatus, duration, description);
                break;
        }
    }
}
