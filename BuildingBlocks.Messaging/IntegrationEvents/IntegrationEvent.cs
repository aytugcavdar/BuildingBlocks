namespace BuildingBlocks.Messaging.IntegrationEvents;

/// <summary>
/// Tüm integration event'lerin türeyeceği base class.
/// IIntegrationEvent implement eder — MassTransit consumer'lar için tip güvenliği sağlar.
/// </summary>
public abstract class IntegrationEvent : IIntegrationEvent
{
    /// <summary>Event'in benzersiz kimliği.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Event'in oluşturulduğu UTC zaman damgası.</summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>Event'i gönderen servis adı (opsiyonel, tracing için).</summary>
    public string? Source { get; init; }

    /// <summary>Correlation ID — request zincirini takip etmek için (opsiyonel).</summary>
    public string? CorrelationId { get; init; }
}
