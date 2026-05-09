namespace BuildingBlocks.Messaging.IntegrationEvents;

/// <summary>
/// Marker contract for integration events used in asynchronous service communication.
/// </summary>
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
    string? CorrelationId { get; }
}
