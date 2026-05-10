using BuildingBlocks.Messaging.EventBus;
using BuildingBlocks.Messaging.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using NSubstitute;

namespace BuildingBlocks.Messaging.Tests.EventBus;

public class MassTransitEventBusTests
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly MassTransitEventBus _eventBus;

    public MassTransitEventBusTests()
    {
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _eventBus = new MassTransitEventBus(_publishEndpoint);
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishIntegrationEvent()
    {
        var integrationEvent = new TestIntegrationEvent
        {
            Name = "catalog-created"
        };
        using var cts = new CancellationTokenSource();

        await _eventBus.PublishAsync(integrationEvent, cts.Token);

        await _publishEndpoint.Received(1).Publish(integrationEvent, cts.Token);
    }

    [Fact]
    public async Task PublishAsync_ShouldThrow_WhenIntegrationEventIsNull()
    {
        Func<Task> act = () => _eventBus.PublishAsync<TestIntegrationEvent>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private sealed class TestIntegrationEvent : IntegrationEvent
    {
        public string Name { get; init; } = string.Empty;
    }
}
