using BuildingBlocks.Messaging.IntegrationEvents;

namespace BuildingBlocks.Messaging.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}
