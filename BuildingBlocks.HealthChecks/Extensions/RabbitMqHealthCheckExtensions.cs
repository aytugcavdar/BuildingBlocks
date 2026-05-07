using BuildingBlocks.HealthChecks.Checks;
using BuildingBlocks.HealthChecks.Core;
using BuildingBlocks.Messaging.MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.HealthChecks.Extensions;

/// <summary>
/// Extension methods for registering RabbitMQ health checks.
/// </summary>
public static class RabbitMqHealthCheckExtensions
{
    /// <summary>
    /// Adds a RabbitMQ health check using RabbitMqOptions from configuration.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configuration">The configuration instance to read RabbitMqOptions from.</param>
    /// <param name="name">The health check name. Default: "rabbitmq".</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string name = "rabbitmq",
        TimeSpan? timeout = null,
        params string[] tags)
    {
        var rabbitMqOptions = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        var connectionString = BuildConnectionString(rabbitMqOptions);

        return builder.AddRabbitMqHealthCheck(connectionString, name, timeout, tags);
    }

    /// <summary>
    /// Adds a RabbitMQ health check using a connection string.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="connectionString">The RabbitMQ connection string (amqp://user:pass@host/vhost).</param>
    /// <param name="name">The health check name. Default: "rabbitmq".</param>
    /// <param name="timeout">The health check timeout. If null, uses default from HealthCheckOptions.</param>
    /// <param name="tags">Additional tags for the health check.</param>
    /// <returns>The health checks builder for fluent chaining.</returns>
    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "rabbitmq",
        TimeSpan? timeout = null,
        params string[] tags)
    {
        var allTags = new List<string>
        {
            HealthCheckTags.MessageBroker,
            HealthCheckTags.Readiness,
            HealthCheckTags.Critical
        };
        allTags.AddRange(tags);

        return builder.AddCheck(
            name: name,
            instance: new RabbitMqHealthCheck(connectionString),
            failureStatus: HealthStatus.Unhealthy,
            tags: allTags,
            timeout: timeout);
    }

    private static string BuildConnectionString(RabbitMqOptions options)
    {
        return $"amqp://{options.UserName}:{options.Password}@{options.Host}{options.VirtualHost}";
    }
}
