using System.Linq.Expressions;
using System.Reflection;
using Ensa.Domain.Common;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Ensa.EntityFrameworkCore.Ambient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories;

/// <summary>
/// Carrier used to produce <b>closure</b> behaviour instead of a constant in manually built expression trees.
/// <para>
/// A value embedded with <see cref="Expression.Constant(object?)"/> is written into the SQL as a
/// <i>literal</i> by EF Core, which produces a separate query plan for every distinct id and bloats the plan
/// cache. When the value is carried as a property of an object, EF automatically turns it into a
/// <b>parameter</b> such as <c>@__id_0</c>.
/// </para>
/// <para>The type is <c>public</c>; the expression tree must not hit an accessibility problem when it is compiled.</para>
/// </summary>
public sealed class QueryParameterHolder<T>(T value)
{
    public T Value { get; } = value;
}

/// <summary>
/// EF Core implementation of <see cref="IReadOnlyRepository{TEntity,TKey}"/>.
/// <para>
/// Every query passes through the global query filters of <see cref="EnsaDbContext"/>: soft-deleted rows and
/// rows belonging to another tenant are removed automatically.
/// </para>
/// </summary>
public class EfCoreReadOnlyRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    public EfCoreReadOnlyRepository(EnsaDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>DbContext access for derived classes (module-specific repositories).</summary>
    protected EnsaDbContext Context { get; }

    /// <summary>The <c>DbSet</c> of this entity.</summary>
    protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

    /// <inheritdoc />
    public virtual IQueryable<TEntity> GetQueryable() => DbSet;

    /// <inheritdoc />
    public virtual IQueryable<TEntity> GetReadOnlyQueryable() => DbSet.AsNoTracking();

    /// <inheritdoc />
    public virtual Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
        => GetQueryable().FirstOrDefaultAsync(BuildIdPredicate(id), cancellationToken);

    /// <inheritdoc />
    public virtual Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => GetQueryable().FirstOrDefaultAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public virtual async Task<TEntity> GetAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        return entity ?? throw new EntityNotFoundException(typeof(TEntity), id);
    }

    /// <inheritdoc />
    public virtual Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<List<TEntity>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (skipCount < 0)
        {
            throw new BusinessException("The number of records to skip cannot be negative.", "Ensa:InvalidPaging");
        }

        if (maxResultCount <= 0)
        {
            throw new BusinessException("The page size must be greater than zero.", "Ensa:InvalidPaging");
        }

        var query = GetQueryable();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        // Skip/Take requires an ordering for deterministic results.
        query = ApplySorting(query, sorting);

        return query.Skip(skipCount).Take(maxResultCount).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<long> GetCountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => GetQueryable().AnyAsync(predicate, cancellationToken);

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>Builds the <c>e =&gt; e.Id == id</c> expression in a parameterised form.</summary>
    protected static Expression<Func<TEntity, bool>> BuildIdPredicate(TKey id)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");

        var idProperty = Expression.Property(parameter, nameof(IEntity<TKey>.Id));

        // A property on a carrier object instead of a constant → EF turns this into a SQL parameter.
        var holder = Expression.Constant(new QueryParameterHolder<TKey>(id));
        var idValue = Expression.Property(holder, nameof(QueryParameterHolder<TKey>.Value));

        return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(idProperty, idValue), parameter);
    }

    /// <summary>
    /// Applies a sorting expression in the form <c>"Name ASC, LastName DESC"</c> to the query.
    /// <para>
    /// Rules:
    /// <list type="bullet">
    /// <item>Every comma-separated part has the form <c>PropertyName [ASC|DESC]</c>.</item>
    /// <item>When no direction is given, <c>ASC</c> is assumed.</item>
    /// <item>Property names are matched case-insensitively.</item>
    /// <item>Nested paths are supported: <c>"Address.City"</c>.</item>
    /// <item>When <c>sorting</c> is empty, <c>Id ASC</c> is applied — paging must be deterministic.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>SECURITY:</b> the sorting expression usually comes from the client. The value is never
    /// concatenated into SQL; only a <see cref="PropertyInfo"/> lookup is performed, and a
    /// <see cref="BusinessException"/> is thrown for a property that cannot be found. This leaves no SQL
    /// injection surface and makes sure an invalid field is not silently ignored.
    /// </para>
    /// </summary>
    protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            return ApplySorting(query, nameof(IEntity<TKey>.Id));
        }

        var parts = sorting.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return ApplySorting(query, nameof(IEntity<TKey>.Id));
        }

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var expression = query.Expression;
        var isFirst = true;

        foreach (var part in parts)
        {
            var tokens = part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var propertyPath = tokens[0];
            var descending = false;

            if (tokens.Length > 2)
            {
                throw new BusinessException(
                    $"Invalid sorting expression: '{part}'. Expected format: 'PropertyName ASC|DESC'.",
                    "Ensa:InvalidSorting");
            }

            if (tokens.Length == 2)
            {
                if (tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    descending = true;
                }
                else if (!tokens[1].Equals("asc", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessException(
                        $"Invalid sort direction: '{tokens[1]}'. Only 'ASC' or 'DESC' can be used.",
                        "Ensa:InvalidSorting");
                }
            }

            Expression body = parameter;
            foreach (var segment in propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var property = body.Type.GetProperty(
                    segment,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property is null)
                {
                    throw new BusinessException(
                        $"'{typeof(TEntity).Name}' has no sortable field named '{propertyPath}'.",
                        "Ensa:InvalidSorting");
                }

                body = Expression.Property(body, property);
            }

            var keySelector = Expression.Lambda(body, parameter);

            var methodName = isFirst
                ? (descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy))
                : (descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy));

            expression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(TEntity), body.Type],
                expression,
                Expression.Quote(keySelector));

            isFirst = false;
        }

        return query.Provider.CreateQuery<TEntity>(expression);
    }
}

/// <summary>Read-only repository shortcut with an <c>int</c> key.</summary>
public class EfCoreReadOnlyRepository<TEntity>(EnsaDbContext context)
    : EfCoreReadOnlyRepository<TEntity, int>(context), IReadOnlyRepository<TEntity>
    where TEntity : class, IEntity<int>;

/// <summary>
/// EF Core implementation of <see cref="IRepository{TEntity,TKey}"/>.
/// <para>
/// <b>Soft delete is not handled here.</b> The repository only calls <c>Remove</c>; converting a physical
/// delete into a logical one for entities implementing <see cref="ISoftDelete"/> happens inside
/// <see cref="EnsaDbContext.ApplyEnsaConcepts"/> at <c>SaveChanges</c> time. That way the rule lives in one
/// place and seed / migration code that touches the DbContext directly gets the same behaviour.
/// </para>
/// <para>
/// When the <b>autoSave</b> parameter is <c>false</c> the change is only written to the ChangeTracker;
/// <see cref="IUnitOfWork.SaveChangesAsync"/> must be called to persist it. That is the normal flow in the
/// AppService layer — one transaction per request.
/// </para>
/// </summary>
public class EfCoreRepository<TEntity, TKey> : EfCoreReadOnlyRepository<TEntity, TKey>, IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    public EfCoreRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
        : base(context)
    {
        DataFilter = dataFilter;
    }

    /// <summary>Used to disable the global filters temporarily (see <see cref="HardDeleteAsync"/>).</summary>
    protected IDataFilter? DataFilter { get; }

    /// <inheritdoc />
    public virtual async Task<TEntity> InsertAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        await SaveIfRequestedAsync(autoSave, cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task InsertManyAsync(
        IEnumerable<TEntity> entities,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
        await SaveIfRequestedAsync(autoSave, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        await SaveIfRequestedAsync(autoSave, cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task UpdateManyAsync(
        IEnumerable<TEntity> entities,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        DbSet.UpdateRange(entities);
        await SaveIfRequestedAsync(autoSave, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        await SaveIfRequestedAsync(autoSave, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>Returns silently when the record does not exist — deletion is idempotent.</remarks>
    public virtual async Task DeleteAsync(
        TKey id,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        await DeleteAsync(entity, autoSave, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task DeleteManyAsync(
        IEnumerable<TEntity> entities,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        DbSet.RemoveRange(entities);
        await SaveIfRequestedAsync(autoSave, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>CAUTION:</b> this method emits a single <c>DELETE ... WHERE</c> statement
    /// (<c>ExecuteDeleteAsync</c>). It is meant for bulk cleanup; it bypasses the ChangeTracker,
    /// the audit fields and <b>soft delete</b> — the rows are really removed from the database.
    /// Use <see cref="DeleteAsync(TEntity,bool,CancellationToken)"/> for a logical delete.
    /// </remarks>
    public virtual Task DeleteDirectAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => GetQueryable().Where(predicate).ExecuteDeleteAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// The entity is added to the <see cref="EnsaDbContext.HardDeletedEntities"/> set, which exempts it
    /// from the soft-delete conversion during <c>SaveChanges</c> and removes it physically. The
    /// soft-delete filter is also disabled for the duration of this call so that an already soft-deleted
    /// record can be removed.
    /// </remarks>
    public virtual async Task HardDeleteAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        using (DataFilter?.Disable<ISoftDelete>() ?? (IDisposable)DisposeAction.Empty)
        {
            Context.HardDeletedEntities.Add(entity);
            DbSet.Remove(entity);
            await SaveIfRequestedAsync(autoSave, cancellationToken);
        }
    }

    /// <summary>Saves the changes when <c>autoSave</c> was requested.</summary>
    protected virtual Task SaveIfRequestedAsync(bool autoSave, CancellationToken cancellationToken)
        => autoSave ? Context.SaveChangesAsync(cancellationToken) : Task.CompletedTask;
}

/// <summary>Repository shortcut with an <c>int</c> key. Module-specific repositories derive from this.</summary>
/// <example>
/// <code>
/// public class CompanyRepository(EnsaDbContext context, IDataFilter dataFilter)
///     : EfCoreRepository&lt;Company&gt;(context, dataFilter), ICompanyRepository
/// {
///     public async Task&lt;CompanyNavigation?&gt; GetWithNavigationAsync(int id) { ... }
/// }
/// </code>
/// </example>
public class EfCoreRepository<TEntity>(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<TEntity, int>(context, dataFilter), IRepository<TEntity>
    where TEntity : class, IEntity<int>;
