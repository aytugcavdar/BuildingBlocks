using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Dynamic;
using BuildingBlocks.Core.Paging;
using System.Linq.Expressions;

namespace BuildingBlocks.Core.Repositories;

/// <summary>
/// Generic async repository sözleşmesi.
/// EF Core implementasyonu Infrastructure katmanındaki EfRepositoryBase'dedir.
/// </summary>
public interface IAsyncRepository<TEntity, TId> : IQuery<TEntity>
    where TEntity : Entity<TId>
{
    // ─── Tekil Sorgular ────────────────────────────────────────────────────────

    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default);

    // ─── Liste Sorgular ─────────────────────────────────────────────────────────

    Task<Paginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int index = 0,
        int size = 10,
        CancellationToken cancellationToken = default);

    Task<Paginate<TEntity>> GetListByDynamicAsync(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int index = 0,
        int size = 10,
        CancellationToken cancellationToken = default);

    // ─── Count / Existence ─────────────────────────────────────────────────────

    Task<int> GetCountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool withDeleted = false,
        CancellationToken cancellationToken = default);

    // ─── CUD Operasyonları ──────────────────────────────────────────────────────

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<ICollection<TEntity>> AddRangeAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<ICollection<TEntity>> UpdateRangeAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default);

    Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false, CancellationToken cancellationToken = default);
    Task<ICollection<TEntity>> DeleteRangeAsync(ICollection<TEntity> entities, bool permanent = false, CancellationToken cancellationToken = default);
}
