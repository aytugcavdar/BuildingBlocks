using BuildingBlocks.HealthChecks.Core;
using BuildingBlocks.HealthChecks.Extensions;
using BuildingBlocks.HealthChecks.Publishers;
using BuildingBlocks.HealthChecks.Telemetry;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.HealthChecks.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksHealthChecks_ShouldBindPreferredBuildingBlocksSection()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("BuildingBlocks:HealthChecks", new Dictionary<string, string?>
        {
            ["EnablePublishers"] = "false",
            ["LivenessEndpoint"] = "/livez",
            ["ReadinessEndpoint"] = "/readyz",
            ["StartupEndpoint"] = "/startupz"
        });

        services.AddBuildingBlocksHealthChecks(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<HealthCheckOptions>();
        options.LivenessEndpoint.Should().Be("/livez");
        options.ReadinessEndpoint.Should().Be("/readyz");
        options.StartupEndpoint.Should().Be("/startupz");
        provider.GetRequiredService<HealthCheckMetrics>().Should().NotBeNull();
        provider.GetServices<IHealthCheckPublisher>().Should().BeEmpty();
    }

    [Fact]
    public void AddBuildingBlocksHealthChecks_ShouldFallbackToLegacyHealthChecksSection()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("HealthChecks", new Dictionary<string, string?>
        {
            ["DefaultCacheIntervalSeconds"] = "45",
            ["EnablePublishers"] = "true"
        });

        services.AddBuildingBlocksHealthChecks(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<HealthCheckOptions>();
        options.DefaultCacheIntervalSeconds.Should().Be(45);
        provider.GetServices<IHealthCheckPublisher>()
            .Should()
            .ContainSingle(publisher => publisher is LoggingHealthCheckPublisher);
    }

    [Fact]
    public void AddBuildingBlocksHealthChecks_ShouldConfigurePublisherPeriod_WhenCachingIsEnabled()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("BuildingBlocks:HealthChecks", new Dictionary<string, string?>
        {
            ["DefaultCacheIntervalSeconds"] = "20",
            ["EnableCaching"] = "true"
        });

        services.AddBuildingBlocksHealthChecks(configuration);
        using var provider = services.BuildServiceProvider();

        var publisherOptions = provider.GetRequiredService<IOptions<HealthCheckPublisherOptions>>().Value;
        publisherOptions.Delay.Should().Be(TimeSpan.FromSeconds(20));
        publisherOptions.Period.Should().Be(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void AddBuildingBlocksHealthChecks_ShouldRejectInvalidTimeout()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("BuildingBlocks:HealthChecks", new Dictionary<string, string?>
        {
            ["DefaultTimeoutSeconds"] = "0"
        });

        var act = () => services.AddBuildingBlocksHealthChecks(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultTimeoutSeconds*");
    }

    private static IConfiguration CreateConfiguration(
        string sectionName,
        IDictionary<string, string?> values)
    {
        var configurationValues = values.ToDictionary(
            pair => $"{sectionName}:{pair.Key}",
            pair => pair.Value);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
    }
}
