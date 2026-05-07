namespace BuildingBlocks.Messaging.IntegrationEvents;

/// <summary>
/// Integration event marker interface.
/// Servisler arası asenkron iletişimde kullanılan eventlerin işaretleyici interface'i.
/// MassTransit consumer'ları bu interface üzerinden tip güvenliği sağlar.
/// </summary>
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
    string? CorrelationId { get; }
}
