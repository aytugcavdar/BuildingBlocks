using BuildingBlocks.Core.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BuildingBlocks.Infrastructure.UnitOfWork;

/// <summary>
/// IUnitOfWork'ün EF Core implementasyonu.
/// Transaction ve savepoint yönetimi sağlar.
/// Not: EfUnitOfWork ile aynı işlevi görür — generic name için burası, tip-spesifik için EfUnitOfWork kullanın.
/// </summary>
public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(TContext context)
    {
        _context = context;
    }

    // ─── Değişiklikleri Kaydet ──────────────────────────────────────────────────
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    // ─── Transaction Yönetimi ───────────────────────────────────────────────────
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already in progress.");

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_transaction != null)
                await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null) return;

        await _transaction.RollbackAsync(cancellationToken);
        _transaction.Dispose();
        _transaction = null;
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
            if (disposing) _transaction?.Dispose();
            _disposed = true;
        }
    }
}
