namespace BuildingBlocks.Core.Domain;

/// <summary>
/// Tüm entity'lerin türediği generic base sınıf.
/// Id, audit alanları ve domain event yönetimini içerir.
/// </summary>
public abstract class Entity<TId> : IEntity<TId>
{
    public TId Id { get; set; } = default!;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>Bu entity'ye ait domain event'lerin read-only koleksiyonu.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
