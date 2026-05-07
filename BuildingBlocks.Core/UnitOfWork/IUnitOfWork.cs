namespace BuildingBlocks.Core.UnitOfWork;

/// <summary>
/// Unit of Work pattern sözleşmesi.
/// Transaction yönetimi ve DB değişikliklerini kaydetme sorumluluğunu taşır.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // ─── Değişiklikleri Kaydet ──────────────────────────────────────────────────
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // ─── Transaction Yönetimi ───────────────────────────────────────────────────
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    // ─── Savepoint (İç içe transaction desteği) ────────────────────────────────
    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
    Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);
}

