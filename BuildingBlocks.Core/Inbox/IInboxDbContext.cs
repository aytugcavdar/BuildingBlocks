using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Core.Inbox;

/// <summary>
/// Projelerin DbContext sınıflarının Idempotent Consumer için InboxMessage nesnelerini 
/// kaydedebileceğini belirten arayüz.
/// </summary>
public interface IInboxDbContext
{
    DbSet<InboxMessage> InboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
