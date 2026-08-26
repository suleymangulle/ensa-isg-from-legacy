using System.Linq.Expressions;
using Ensa.Domain.Common;

namespace Ensa.Domain.Repositories;

/// <summary>Marker interface used by the DI assembly scan.</summary>
public interface IRepository;

/// <summary>Read-only repository contract.</summary>
public interface IReadOnlyRepository<TEntity, TKey> : IRepository
    where TEntity : class, IEntity<TKey>
{
    /// <summary>Tracked query with the tenant and soft-delete filters already applied.</summary>
    IQueryable<TEntity> GetQueryable();

    /// <summary>Untracked (<c>AsNoTracking</c>) query, for read-only paths.</summary>
    IQueryable<TEntity> GetReadOnlyQueryable();

    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Throws <c>EntityNotFoundException</c> when the record does not exist.</summary>
    Task<TEntity> GetAsync(TKey id, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}

/// <summary>Repository contract that can also write.</summary>
public interface IRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

    Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

    Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes when the entity supports it; deletes physically otherwise.</summary>
    Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

    Task DeleteAsync(TKey id, bool autoSave = false, CancellationToken cancellationToken = default);

    Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);

    Task DeleteDirectAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Removes the row physically, ignoring soft delete.</summary>
    Task HardDeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);
}

/// <summary>Shorthand for an <c>int</c> key.</summary>
public interface IRepository<TEntity> : IRepository<TEntity, int>
    where TEntity : class, IEntity<int>;

/// <summary>Read-only shorthand for an <c>int</c> key.</summary>
public interface IReadOnlyRepository<TEntity> : IReadOnlyRepository<TEntity, int>
    where TEntity : class, IEntity<int>;
