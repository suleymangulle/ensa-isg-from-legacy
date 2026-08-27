using Ensa.DataMigrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Rebuilds the legacy-to-modern id map for the two plan line tables, which were written without
/// one.
/// <para>
/// <b>Why they have no map.</b> <c>PlanStep</c> writes 1,045,151 work plan lines and 894,571
/// training plan lines through <see cref="BulkWriter"/> with a watermark. That was the right call
/// for the volume, and it was made on the understanding that nothing would ever need to look a
/// plan line up by its legacy id. The document link tables do:
/// <c>FirmaPersonelDosya_T.EgitimPlaniSatiriId</c> is the link between an employee's certificate
/// and the training session that produced it, and dropping it loses a fact the legacy system
/// records.
/// </para>
/// <para>
/// <b>How it is rebuilt.</b> The bulk write inserted rows into an empty identity column in strict
/// legacy id order and nothing else has ever written those tables, so the modern id of a written
/// row is its rank in that order. Replaying the same legacy query with the same orphan test
/// reproduces the sequence exactly.
/// </para>
/// <para>
/// <b>Why that is safe to rely on.</b> Because it is not relied on — it is checked. A replay that
/// is wrong is wrong silently and catastrophically: every certificate would attach to somebody
/// else's training. So the step refuses to write anything unless the replay produces exactly as
/// many rows as the destination holds AND the destination ids run contiguously from one. Either
/// check failing means the assumption no longer holds, and the honest outcome is no map rather
/// than a plausible wrong one — the link steps then drop those references and say so.
/// </para>
/// <para>
/// This is scaffolding for a migration that was arrived at in stages, not a pattern to copy. A
/// table written from scratch today would capture its identities as it goes.
/// </para>
/// </summary>
public sealed class PlanLineMapStep : IMigrationStep
{
    public int Order => 85;

    public string Name => "plan-line-map";

    public string Description => "Rebuilds the id map for the two bulk-written plan line tables";

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var planMap = await context.IdMap.LoadAsync("CalismaPlani_T", cancellationToken);
        var activityMap = await context.IdMap.LoadAsync("Aktivite_T", cancellationToken);
        var trainingPlanMap = await context.IdMap.LoadAsync("EgitimPlani_T", cancellationToken);
        var trainingMap = await context.IdMap.LoadAsync("Egitim_T", cancellationToken);

        // The orphan tests below are the ones PlanStep applies, and they have to stay that way.
        // They are repeated rather than shared because sharing them would mean exposing the
        // projections, and a lambda reached from two places is the easier thing to break quietly;
        // the count check is what actually catches a divergence.
        var work = await RebuildAsync(
            context,
            "CalismaPlaniSatirlari_T",
            "ensa.WorkPlanLine",
            """
            SELECT CalismaPlaniSatirId, CalismaPlaniId, AktiviteId, FirmaId
            FROM CalismaPlaniSatirlari_T ORDER BY CalismaPlaniSatirId;
            """,
            "CalismaPlaniSatirId",
            reader => planMap.ContainsKey(Required(reader, "CalismaPlaniId"))
                      && activityMap.ContainsKey(Required(reader, "AktiviteId"))
                      && companyMap.ContainsKey(Required(reader, "FirmaId")),
            cancellationToken);

        var training = await RebuildAsync(
            context,
            "EgitimPlaniSatirlari_T",
            "ensa.TrainingPlanLine",
            """
            SELECT EgitimPlaniSatirId, EgitimPlaniId, EgitimId
            FROM EgitimPlaniSatirlari_T ORDER BY EgitimPlaniSatirId;
            """,
            "EgitimPlaniSatirId",
            reader => trainingPlanMap.ContainsKey(Required(reader, "EgitimPlaniId"))
                      && trainingMap.ContainsKey(Required(reader, "EgitimId")),
            cancellationToken);

        return new StepResult(
            work.Read + training.Read,
            work.Written + training.Written,
            work.Skipped + training.Skipped,
            string.Join("; ", new[] { work.Note, training.Note }.Where(note => note is not null)));
    }

    private static async Task<StepResult> RebuildAsync(
        MigrationContext context,
        string legacyTable,
        string modernTable,
        string sql,
        string keyColumn,
        Func<SqlDataReader, bool> written,
        CancellationToken cancellationToken)
    {
        var already = await context.IdMap.LoadAsync(legacyTable, cancellationToken);
        if (already.Count > 0)
        {
            return new StepResult(0, 0, 0, $"{legacyTable}: already mapped ({already.Count})");
        }

        var (count, minimum, maximum) = await DestinationShapeAsync(context, modernTable, cancellationToken);

        if (count == 0)
        {
            return new StepResult(0, 0, 0, $"{legacyTable}: REFUSED — {modernTable} is empty, run the plans step first");
        }

        if (minimum != 1 || maximum != count)
        {
            return new StepResult(
                0, 0, count,
                $"{legacyTable}: REFUSED — {modernTable} ids run {minimum}..{maximum} over {count} rows, "
                + "so they are not the untouched insert order this rebuild depends on");
        }

        var read = 0;
        var pairs = new List<(int LegacyId, int ModernId)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                if (written(reader))
                {
                    // The rank is the modern id, because the destination ids were proved above to
                    // run 1..count with no gaps.
                    pairs.Add((Required(reader, keyColumn), pairs.Count + 1));
                }
            }
        }

        if (pairs.Count != count)
        {
            return new StepResult(
                read, 0, read,
                $"{legacyTable}: REFUSED — the replay kept {pairs.Count} rows but {modernTable} holds "
                + $"{count}. The orphan test no longer matches the one that wrote the table, and a "
                + "map built on it would attach every row to the wrong record");
        }

        if (context.DryRun)
        {
            return new StepResult(read, pairs.Count, read - pairs.Count,
                $"{legacyTable}: {pairs.Count} pair(s) reproduced and verified — dry run writes none");
        }

        await context.IdMap.SaveAsync(legacyTable, pairs, 'I', cancellationToken);

        context.Logger.LogInformation(
            "    {Table}: {Count} id(s) mapped, verified against {Modern}", legacyTable, pairs.Count, modernTable);

        return new StepResult(read, pairs.Count, read - pairs.Count,
            $"{legacyTable}: {pairs.Count} mapped, count and contiguity verified against {modernTable}");
    }

    /// <summary>How many rows the destination holds, and the range its identities cover.</summary>
    private static async Task<(int Count, int Minimum, int Maximum)> DestinationShapeAsync(
        MigrationContext context,
        string modernTable,
        CancellationToken cancellationToken)
    {
        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var command = new SqlCommand(
            $"SELECT COUNT(*), ISNULL(MIN(Id), 0), ISNULL(MAX(Id), 0) FROM {modernTable};",
            connection) { CommandTimeout = 600 };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
    }

    private static async Task<Dictionary<int, int>> LoadCompanyMapAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, int>(await context.IdMap.LoadAsync("Firma_T", cancellationToken));

        foreach (var (legacyId, modernId) in
                 await context.IdMap.LoadAsync("Firma_T:KurumSirket", cancellationToken))
        {
            map[legacyId] = modernId;
        }

        return map;
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));
}
