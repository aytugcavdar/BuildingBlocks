namespace BuildingBlocks.Messaging.IntegrationEvents;

/// <summary>
/// Base type for integration events published between services.
/// </summary>
public abstract class IntegrationEvent : IIntegrationEvent
{
    /// <summary>Unique event identifier.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>UTC timestamp for when the event instance was created.</summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>Optional source service name for tracing and diagnostics.</summary>
    public string? Source { get; init; }

    /// <summary>Optional correlation identifier for request and message flow tracing.</summary>
    public string? CorrelationId { get; init; }
}
