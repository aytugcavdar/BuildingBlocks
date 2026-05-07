namespace BuildingBlocks.Core.Outbox;

/// <summary>
/// Outbox pattern mesaj sözleşmesi.
/// Persist edilip async olarak publish edilecek tüm mesajların implement etmesi gerekir.
/// </summary>
public interface IOutboxMessage
{
    Guid Id { get; }
    string Type { get; }
    string Content { get; }
    DateTime OccurredOn { get; }
    DateTime? ProcessedOn { get; }
    string? Error { get; }
    int RetryCount { get; }
    bool IsProcessed { get; }
    bool IsFailed { get; }

    void MarkAsProcessed();
    void MarkAsFailed(string error);
}
