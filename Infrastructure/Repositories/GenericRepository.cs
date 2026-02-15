namespace Infrastructure.Repositories;

using Domain.Common;
using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<GenericRepository<T>> _logger;

    public GenericRepository(AppDbContext context, ILogger<GenericRepository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAsync(
        ISpecification<T> spec,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(spec);
        if (spec.AsNoTracking)
        {
            query = query.AsNoTracking();
        }
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetEntityWithSpec(
        ISpecification<T> spec,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(spec);
        if (spec.AsNoTracking)
        {
            query = query.AsNoTracking();
        }
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        ISpecification<T> spec,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).CountAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _dbSet.AddAsync(entity, cancellationToken);
        _logger.LogDebug("Entity {EntityType} with Id {Id} added", typeof(T).Name, entity.Id);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }
        entry.State = EntityState.Modified;
        _logger.LogDebug("Entity {EntityType} with Id {Id} updated", typeof(T).Name, entity.Id);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        _logger.LogDebug("Entity {EntityType} with Id {Id} deleted", typeof(T).Name, entity.Id);
        return Task.CompletedTask;
    }

    public virtual async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            await UpdateAsync(entity, cancellationToken);
            _logger.LogInformation("Entity {EntityType} with Id {Id} soft deleted", typeof(T).Name, id);
        }
    }

    public virtual async IAsyncEnumerable<T> GetAllStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var entity in _dbSet
            .AsNoTracking()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return entity;
        }
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(
        ISpecification<T> spec,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).AnyAsync(cancellationToken);
    }

    public virtual async Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        var now = DateTime.UtcNow;
        foreach (var entity in entitiesList)
        {
            entity.CreatedAt = now;
        }
        await _dbSet.AddRangeAsync(entitiesList, cancellationToken);
        _logger.LogInformation("Batch added {Count} entities of type {EntityType}", entitiesList.Count, typeof(T).Name);
    }

    public virtual Task UpdateRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        var now = DateTime.UtcNow;
        foreach (var entity in entitiesList)
        {
            entity.UpdatedAt = now;
        }
        _dbSet.UpdateRange(entitiesList);
        _logger.LogInformation("Batch updated {Count} entities of type {EntityType}", entitiesList.Count, typeof(T).Name);
        return Task.CompletedTask;
    }

    public virtual Task DeleteRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        _dbSet.RemoveRange(entitiesList);
        _logger.LogInformation("Batch deleted {Count} entities of type {EntityType}", entitiesList.Count, typeof(T).Name);
        return Task.CompletedTask;
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        ISpecification<T>? spec = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = spec != null ? ApplySpecification(spec) : _dbSet.AsQueryable();
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .AsNoTracking()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber < totalPages
        };
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {
        return SpecificationEvaluator<T>.GetQuery(_dbSet.AsQueryable(), spec);
    }
}
