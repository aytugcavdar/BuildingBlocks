using BuildingBlocks.HealthChecks.Core;
using BuildingBlocks.HealthChecks.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.HealthChecks.Tests.Extensions;

public class RabbitMqHealthCheckExtensionsTests
{
    [Fact]
    public void AddRabbitMqHealthCheck_ShouldRegisterRabbitMqCheckFromBuildingBlocksMessagingConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("BuildingBlocks:Messaging:RabbitMQ");
        var builder = services.AddHealthChecks();

        builder.AddRabbitMqHealthCheck(configuration, "message-broker", tags: "custom");
        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations;

        registrations.Should().ContainSingle(registration =>
            registration.Name == "message-broker" &&
            registration.Tags.Contains(HealthCheckTags.MessageBroker) &&
            registration.Tags.Contains(HealthCheckTags.Readiness) &&
            registration.Tags.Contains(HealthCheckTags.Critical) &&
            registration.Tags.Contains("custom"));
    }

    [Fact]
    public void AddRabbitMqHealthCheck_ShouldFallbackToLegacyRabbitMqConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("RabbitMQ");
        var builder = services.AddHealthChecks();

        builder.AddRabbitMqHealthCheck(configuration);
        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations;

        registrations.Should().ContainSingle(registration => registration.Name == "rabbitmq");
    }

    private static IConfiguration CreateConfiguration(string sectionName)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{sectionName}:Host"] = "rabbitmq",
            [$"{sectionName}:VirtualHost"] = "/microservice",
            [$"{sectionName}:UserName"] = "user",
            [$"{sectionName}:Password"] = "pass"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
