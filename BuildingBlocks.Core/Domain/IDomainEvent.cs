using MediatR;

namespace BuildingBlocks.Core.Domain;

/// <summary>
/// Domain event marker interface.
/// MediatR INotification'dan türer — handler'lar MediatR pipeline üzerinden tetiklenir.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>Event'in oluşturulduğu UTC zaman damgası.</summary>
    DateTime OccurredOn { get; }
}
