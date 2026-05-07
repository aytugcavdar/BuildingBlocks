using BuildingBlocks.Core.Domain;

namespace BuildingBlocks.Core.Outbox;

/// <summary>
/// Transactional Outbox pattern için mesaj entity'si.
/// Domain event'leri DB'ye persist edip garantili delivery sağlar.
/// </summary>
public class OutboxMessage : Entity<Guid>, IOutboxMessage
{
    /// <summary>Event'in tam tipi (AssemblyQualifiedName)</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Event'in JSON serialize edilmiş içeriği</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Event'in oluşturulduğu UTC zaman damgası</summary>
    public DateTime OccurredOn { get; set; }

    /// <summary>Event'in işlendiği UTC zaman damgası (null = henüz işlenmedi)</summary>
    public DateTime? ProcessedOn { get; set; }

    /// <summary>Son hata mesajı (varsa)</summary>
    public string? Error { get; set; }

    /// <summary>Kaç kez denendi</summary>
    public int RetryCount { get; set; }

    /// <summary>EF Core için parameteresiz constructor</summary>
    public OutboxMessage() { }

    /// <summary>Yeni outbox mesajı oluşturur</summary>
    public OutboxMessage(string type, string content)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        OccurredOn = DateTime.UtcNow;
        CreatedDate = DateTime.UtcNow;
        RetryCount = 0;
    }

    /// <summary>Mesajı başarılı olarak işaretler</summary>
    public void MarkAsProcessed()
    {
        ProcessedOn = DateTime.UtcNow;
        UpdatedDate = DateTime.UtcNow;
    }

    /// <summary>Mesajı başarısız olarak işaretler ve retry sayacını artırır</summary>
    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
        UpdatedDate = DateTime.UtcNow;
    }

    /// <summary>Mesaj başarıyla işlendi mi?</summary>
    public bool IsProcessed => ProcessedOn.HasValue;

    /// <summary>Mesaj maksimum retry sayısına ulaştı mı?</summary>
    public bool IsFailed => RetryCount >= OutboxOptions.DefaultMaxRetryCount;
}