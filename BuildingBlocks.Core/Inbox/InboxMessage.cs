namespace BuildingBlocks.Core.Inbox;

/// <summary>
/// Idempotent Consumer (Inbox Pattern) için kullanılan entity.
/// Gelen her IntegrationEvent'in MessageId'sini (Guid) tutar, aynı mesaj tekrar gelirse
/// veritabanında var olduğu için işlemeyi atlar (Idempotency).
/// </summary>
public class InboxMessage
{
    /// <summary>
    /// Gelen mesajın eşsiz kimliği (MessageId veya EventId)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event'in Tipi (Örn: OrderCreatedIntegrationEvent)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mesajın işlendiği tarih
    /// </summary>
    public DateTime ProcessedOn { get; set; }
}
