using Ensa.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ensa.EntityFrameworkCore.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>.
/// <para>
/// <b>Scope:</b> registered as scoped; one HTTP request = one <see cref="EnsaDbContext"/> = one unit of work.
/// When the repositories run with <c>autoSave: false</c>, every change reaches the database in a single
/// <c>SaveChanges</c> and EF Core executes it atomically in its own implicit transaction.
/// </para>
/// <para>
/// <b>When is an explicit transaction needed?</b> Only when <i>more than one</i> <c>SaveChanges</c> call, or a
/// <c>SaveChanges</c> together with raw SQL / a bulk operation (<c>ExecuteDelete</c>, <c>ExecuteUpdate</c>),
/// must form a single atomic block. Calling <see cref="BeginTransactionAsync"/> for a single
/// <c>SaveChanges</c> is unnecessary.
/// </para>
/// </summary>
public class UnitOfWork(EnsaDbContext context) : IUnitOfWork
{
    private readonly EnsaDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public bool HasActiveTransaction => _context.Database.CurrentTransaction is not null;

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// Nested calls do not open a new database transaction; they return a wrapper that joins the outer
    /// transaction. The inner <c>CommitAsync</c> does nothing — the commit decision always belongs to the
    /// outermost scope. The inner <c>RollbackAsync</c>, on the other hand, rolls back the whole
    /// transaction; partial rollback (savepoint) semantics are deliberately not supported, because a
    /// "partly rolled back" state is a silent source of inconsistency for business rules.
    /// </remarks>
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var existing = _context.Database.CurrentTransaction;
        if (existing is not null)
        {
            return new NestedUnitOfWorkTransaction(existing);
        }

        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfCoreUnitOfWorkTransaction(transaction);
    }
}

/// <summary>Unit wrapping a real EF Core transaction.</summary>
internal sealed class EfCoreUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction = transaction;
    private bool _completed;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken);
        _completed = true;
    }

    /// <summary>
    /// Rolls the transaction back if it is disposed without being committed.
    /// This is the correct behaviour when a <c>using</c> block is left through an exception.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch (InvalidOperationException)
            {
                // The connection may already be closed; errors during dispose are swallowed.
            }
        }

        await _transaction.DisposeAsync();
    }
}

/// <summary>
/// Inner scope opened while a transaction is already open.
/// Commit is a no-op, because commit ownership belongs to the outermost scope.
/// </summary>
internal sealed class NestedUnitOfWorkTransaction(IDbContextTransaction ambient) : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _ambient = ambient;

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _ambient.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
