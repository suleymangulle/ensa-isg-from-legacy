using Ensa.Domain.Common;
using Ensa.EntityFrameworkCore;
using Ensa.EntityFrameworkCore.Ambient;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Infrastructure;

/// <summary>What a step reported doing, so the runner can print a reconciliation.</summary>
/// <param name="Read">Rows read from the legacy database.</param>
/// <param name="Written">Rows inserted or updated in the destination.</param>
/// <param name="Skipped">Rows deliberately not carried over, for reasons the step explains.</param>
/// <param name="Note">One line for the log: what was skipped and why.</param>
public readonly record struct StepResult(int Read, int Written, int Skipped, string? Note = null)
{
    public static StepResult Nothing => new(0, 0, 0);
}

/// <summary>
/// One migration step: one legacy table, or one closely bound group of them.
/// <para>
/// A step must be <b>idempotent</b>. It is run more than once — that is the normal way a migration
/// of this size is arrived at, not a failure mode — so it looks up what it already produced in
/// <see cref="IdMap"/> and updates rather than inserting a second copy.
/// </para>
/// </summary>
public interface IMigrationStep
{
    /// <summary>Execution order. Steps that a later step's foreign keys depend on come first.</summary>
    int Order { get; }

    /// <summary>Short name, used by <c>--step</c> and in the log.</summary>
    string Name { get; }

    /// <summary>What this step carries over, in one sentence, for the run summary.</summary>
    string Description { get; }

    Task<StepResult> RunAsync(MigrationContext context, CancellationToken cancellationToken = default);
}

/// <summary>Everything a step needs: both connections, the id map, the clock and a logger.</summary>
public sealed class MigrationContext(
    MigrationTarget target,
    IdMap idMap,
    ILogger logger,
    bool dryRun)
{
    public MigrationTarget Target { get; } = target;

    public IdMap IdMap { get; } = idMap;

    public ILogger Logger { get; } = logger;

    /// <summary>When set, steps read and report but write nothing.</summary>
    public bool DryRun { get; } = dryRun;

    /// <summary>
    /// Fits legacy values into the destination columns and counts what had to be shortened.
    /// Shared, so the limits are read from the schema once per run.
    /// </summary>
    public FieldFitter Fitter { get; } = new();

    /// <summary>
    /// Streams rows straight into a table, for the ones too large for Entity Framework. Refuses a
    /// table with an encrypted column - see <see cref="BulkWriter"/>.
    /// </summary>
    public BulkWriter Bulk { get; } = new(target.ModernConnectionString);

    /// <summary>
    /// Stand-in identity for a row a dry run would have inserted.
    /// <para>
    /// A dry run writes nothing, so the rows it intends to create have no identity - and every
    /// later stage, which finds its parents through the id map, would report its entire input as
    /// orphaned. The rehearsal has to be believable or nobody runs it.
    /// </para>
    /// <para>
    /// Negative on purpose: a real identity column never produces one, so a placeholder that
    /// escaped into a write would break a foreign key at once rather than quietly attach a row to
    /// something unrelated.
    /// </para>
    /// </summary>
    public int NextDryRunId() => Interlocked.Decrement(ref _dryRunCounter);

    private int _dryRunCounter;

    /// <summary>Opens a connection to the legacy database.</summary>
    public async Task<SqlConnection> OpenLegacyAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(Target.LegacyConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>Opens a connection to the destination.</summary>
    public async Task<SqlConnection> OpenModernAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(Target.ModernConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// Builds an <see cref="EnsaDbContext"/> for writing entities.
    /// <para>
    /// <b>Why not raw SQL.</b> Several columns - national identity numbers, tax numbers, the Medula
    /// credentials - go through a deterministic AES value converter. A hand-written INSERT stores
    /// the plaintext in a column every reader will try to decrypt: the value is both exposed and
    /// unreadable, and nothing errors. Writing through the context applies the converters.
    /// </para>
    /// <para>
    /// <b>Context.</b> Host user, no ambient tenant, and both the multi-tenant and company-scope
    /// filters disabled: a migration covers every organization in one pass. <c>TenantId</c> is set
    /// explicitly on each row, which the <c>SaveChanges</c> interceptor leaves alone - it only
    /// stamps the ambient tenant when the value is still null.
    /// </para>
    /// </summary>
    public EnsaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EnsaDbContext>()
            .UseSqlServer(Target.ModernConnectionString, sql => sql.CommandTimeout(600))
            .Options;

        return new EnsaDbContext(
            options,
            NullCurrentTenant.Instance,
            NullCurrentUser.Instance,
            new Clock(),
            DataFilter);
    }

    /// <summary>
    /// The filter switch shared by every context this migration creates.
    /// <para>
    /// Held on the context rather than created per call because <c>DataFilter</c> is
    /// <c>AsyncLocal</c>-backed: a <c>Disable</c> scope opened by a step has to be visible to the
    /// DbContext the step then creates.
    /// </para>
    /// </summary>
    public IDataFilter DataFilter { get; } = new DataFilter();

    /// <summary>Opens the isolation switches a migration needs, for the enclosing scope.</summary>
    public IDisposable EnterMigrationScope()
        => new CompositeDisposable(
            DataFilter.Disable<IMultiTenant>(),
            DataFilter.Disable<ICompanyScoped>(),
            DataFilter.Disable<ISoftDelete>());

    private sealed class CompositeDisposable(params IDisposable[] parts) : IDisposable
    {
        public void Dispose()
        {
            for (var index = parts.Length - 1; index >= 0; index--)
            {
                parts[index].Dispose();
            }
        }
    }

    /// <summary>Reads rows from the legacy database, one at a time, without buffering them all.</summary>
    public async IAsyncEnumerable<SqlDataReader> ReadLegacyAsync(
        string sql,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenLegacyAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 600 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return reader;
        }
    }
}
