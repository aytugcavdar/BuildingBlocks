using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Dynamic;
using BuildingBlocks.Core.Paging;
using BuildingBlocks.Core.Repositories;
using BuildingBlocks.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BuildingBlocks.Infrastructure.EntityFramework;

/// <summary>
/// IAsyncRepository'nin EF Core implementasyonu.
/// Soft delete, pagination, dynamic filter, AnyAsync ve GetCountAsync destekler.
/// </summary>
public class EfRepositoryBase<TEntity, TId, TDbContext> : IAsyncRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TDbContext : DbContext
{
    protected readonly TDbContext _dbContext;

    public EfRepositoryBase(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<TEntity> Query() => _dbContext.Set<TEntity>();

    // ─── Tekil Sorgular ──────────────────────────────────────────────────────────

    public async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var queryable = Query();

        if (!enableTracking) queryable = queryable.AsNoTracking();

        // Tutarlı soft delete filtresi
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        else
            queryable = queryable.Where(e => e.DeletedDate == null);

        if (include != null) queryable = include(queryable);

        return await queryable.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    // ─── Liste Sorgular ───────────────────────────────────────────────────────────

    public async Task<Paginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int index = 0,
        int size = 10,
        CancellationToken cancellationToken = default)
    {
        var queryable = Query();

        if (!enableTracking) queryable = queryable.AsNoTracking();
        if (include != null) queryable = include(queryable);

        // Tutarlı soft delete filtresi
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        else
            queryable = queryable.Where(e => e.DeletedDate == null);

        if (predicate != null) queryable = queryable.Where(predicate);

        return orderBy != null
            ? await orderBy(queryable).ToPaginateAsync(index, size, cancellationToken)
            : await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    public async Task<Paginate<TEntity>> GetListByDynamicAsync(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int index = 0,
        int size = 10,
        CancellationToken cancellationToken = default)
    {
        var queryable = Query().ToDynamic(dynamic);

        if (!enableTracking) queryable = queryable.AsNoTracking();
        if (include != null) queryable = include(queryable);

        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        else
            queryable = queryable.Where(e => e.DeletedDate == null);

        if (predicate != null) queryable = queryable.Where(predicate);

        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    // ─── Count / Existence ────────────────────────────────────────────────────────

    public async Task<int> GetCountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var queryable = Query().AsNoTracking();

        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        else
            queryable = queryable.Where(e => e.DeletedDate == null);

        return predicate != null
            ? await queryable.CountAsync(predicate, cancellationToken)
            : await queryable.CountAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool withDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var queryable = Query().AsNoTracking();

        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        else
            queryable = queryable.Where(e => e.DeletedDate == null);

        return await queryable.AnyAsync(predicate, cancellationToken);
    }

    // ─── CUD Operasyonları ────────────────────────────────────────────────────────

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await _dbContext.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task<ICollection<TEntity>> AddRangeAsync(
        ICollection<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
            entity.CreatedDate = DateTime.UtcNow;

        await _dbContext.AddRangeAsync(entities, cancellationToken);
        return entities;
    }

    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedDate = DateTime.UtcNow;
        _dbContext.Update(entity);
        return Task.FromResult(entity);
    }

    public Task<ICollection<TEntity>> UpdateRangeAsync(
        ICollection<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
            entity.UpdatedDate = DateTime.UtcNow;

        _dbContext.UpdateRange(entities);
        return Task.FromResult(entities);
    }

    public Task<TEntity> DeleteAsync(
        TEntity entity,
        bool permanent = false,
        CancellationToken cancellationToken = default)
    {
        if (permanent)
            _dbContext.Remove(entity);
        else
        {
            entity.DeletedDate = DateTime.UtcNow;
            _dbContext.Update(entity);
        }
        return Task.FromResult(entity);
    }

    public Task<ICollection<TEntity>> DeleteRangeAsync(
        ICollection<TEntity> entities,
        bool permanent = false,
        CancellationToken cancellationToken = default)
    {
        if (permanent)
            _dbContext.RemoveRange(entities);
        else
        {
            foreach (var entity in entities)
                entity.DeletedDate = DateTime.UtcNow;
            _dbContext.UpdateRange(entities);
        }
        return Task.FromResult(entities);
    }
}