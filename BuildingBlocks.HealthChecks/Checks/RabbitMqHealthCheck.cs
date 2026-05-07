using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace BuildingBlocks.HealthChecks.Checks;

/// <summary>
/// Health check for RabbitMQ connectivity.
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RabbitMqHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            
            if (connection.IsOpen)
            {
                return HealthCheckResult.Healthy("RabbitMQ connection is healthy");
            }

            return HealthCheckResult.Unhealthy("RabbitMQ connection is not open");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ health check failed",
                exception: ex);
        }
    }
}
