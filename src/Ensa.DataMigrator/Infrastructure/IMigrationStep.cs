using Microsoft.Data.SqlClient;
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
