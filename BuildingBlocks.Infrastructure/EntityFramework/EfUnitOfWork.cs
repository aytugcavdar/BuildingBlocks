using BuildingBlocks.Core.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BuildingBlocks.Infrastructure.EntityFramework;

/// <summary>
/// IUnitOfWork'ün EF Core implementasyonu.
/// Savepoint desteği dahil tam transaction yönetimi sağlar.
/// </summary>
public class EfUnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public EfUnitOfWork(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ─── Değişiklikleri Kaydet ──────────────────────────────────────────────────
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);

    // ─── Transaction Yönetimi ───────────────────────────────────────────────────
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already in progress.");

        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null) return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // ─── Savepoint ──────────────────────────────────────────────────────────────
    public async Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Cannot create savepoint without an active transaction.");

        await _transaction.CreateSavepointAsync(name, cancellationToken);
    }

    public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Cannot rollback to savepoint without an active transaction.");

        await _transaction.RollbackToSavepointAsync(name, cancellationToken);
    }

    public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Cannot release savepoint without an active transaction.");

        await _transaction.ReleaseSavepointAsync(name, cancellationToken);
    }

    // ─── Dispose ────────────────────────────────────────────────────────────────
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                _transaction?.Dispose();
            _disposed = true;
        }
    }
}
