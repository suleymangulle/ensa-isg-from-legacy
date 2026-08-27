using Microsoft.Extensions.Logging;
using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The visit record: 1,733,816 rows, the largest table this migration carries.
/// <para>
/// Every visit a specialist or physician made to a workplace. It is the evidence that the service
/// was delivered, so it is not one of the tables that can be left behind.
/// </para>
/// <para>
/// <b>Written in bulk, not through the model.</b> At the DbContext's ~340 rows a second this table
/// alone is an hour and a half. It has no encrypted column and nothing points at it, so neither of
/// the two reasons to go through Entity Framework applies — and <see cref="BulkWriter"/> refuses
/// the table outright if that ever stops being true.
/// </para>
/// <para>
/// <b>Resumed with a watermark, not an id map.</b> Nothing will ever look a visit up by its legacy
/// id, so building a map of 1.7 million translations would be work nobody uses. Reading in id
/// order, remembering how far it got is enough for a re-run to continue rather than duplicate.
/// </para>
/// </summary>
public sealed class VisitStep : IMigrationStep
{
    public int Order => 50;

    public string Name => "visits";

    public string Description => "Every visit made to a workplace (1.7M rows, bulk-loaded)";

    /// <summary>Legacy rows read per pass before the watermark moves.</summary>
    private const int ChunkSize = 100_000;

    private static readonly string[] Columns =
    [
        "CompanyId", "UserId", "VisitDate", "Start", "End", "OperationType", "Description",
        "Color", "ScheduledWeek", "ScheduledMonth", "RegionCode", "OtherCompanyDistanceKm",
        "Completed", "CreationTime", "IsDeleted", "TenantId",
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        await using (var model = context.CreateDbContext())
        {
            BulkWriter.EnsureNoConverters(model, "Visit");
        }

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        var watermark = await context.IdMap.GetWatermarkAsync("Ziyaret_T", cancellationToken);
        var startedAt = watermark;

        var read = 0;
        var written = 0;
        var orphaned = 0;

        while (true)
        {
            var (chunkRead, chunkWritten, chunkOrphaned, lastId) = await CopyChunkAsync(
                context, companyMap, userMap, organizationMap, watermark, cancellationToken);

            read += chunkRead;
            written += chunkWritten;
            orphaned += chunkOrphaned;

            if (chunkRead == 0)
            {
                break;
            }

            watermark = lastId;

            if (!context.DryRun)
            {
                // Moved after each chunk rather than at the end: a run that dies halfway has to
                // leave the mark where the data actually stops, or the next run repeats or skips.
                await context.IdMap.SetWatermarkAsync("Ziyaret_T", watermark, cancellationToken);
            }

            context.Logger.LogInformation(
                "    visits: {Written} written so far (legacy id {Watermark})", written, watermark);

            if (context.DryRun)
            {
                break;
            }
        }

        var note = $"visits: {written} written";
        if (startedAt > 0)
        {
            note += $" (resumed from legacy id {startedAt})";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (company or user missing)";
        }

        if (context.DryRun)
        {
            note += " — dry run stops after one chunk";
        }

        return new StepResult(read, written, orphaned, note);
    }

    private static async Task<(int Read, int Written, int Orphaned, int LastId)> CopyChunkAsync(
        MigrationContext context,
        Dictionary<int, int> companyMap,
        Dictionary<int, int> userMap,
        Dictionary<int, int> organizationMap,
        int watermark,
        CancellationToken cancellationToken)
    {
        var read = 0;
        var orphaned = 0;
        var lastId = watermark;

        async IAsyncEnumerable<object?[]> RowsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken token)
        {
            const string sql = """
                SELECT TOP (@take) ZiyaretId, FirmaId, KullaniciId, ZiyaretTarihi, Baslangic, Bitis,
                       Aciklama, Renk, ProgramlananHafta, ProgramlananAy, BolgeKodu,
                       DigerFirmaUzaklik, EklemeTarihi, KurumId
                FROM Ziyaret_T
                WHERE ZiyaretId > @after
                ORDER BY ZiyaretId;
                """;

            await using var connection = await context.OpenLegacyAsync(token);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 };
            command.Parameters.AddWithValue("@take", ChunkSize);
            command.Parameters.AddWithValue("@after", watermark);

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                read++;
                lastId = Required(reader, "ZiyaretId");

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !userMap.TryGetValue(Required(reader, "KullaniciId"), out var userId))
                {
                    orphaned++;
                    continue;
                }

                yield return
                [
                    companyId,
                    userId,
                    Date(reader, "ZiyaretTarihi") ?? DateTime.Now,
                    Date(reader, "Baslangic"),
                    Date(reader, "Bitis"),
                    // IslemTuru is empty in all 1,733,816 legacy rows; there is nothing to map.
                    (int)VisitType.Unspecified,
                    Fit(context, "Visit", "Description", Text(reader, "Aciklama")),
                    Fit(context, "Visit", "Color", Text(reader, "Renk")),
                    Int(reader, "ProgramlananHafta"),
                    Int(reader, "ProgramlananAy"),
                    Int(reader, "BolgeKodu"),
                    Distance(reader, "DigerFirmaUzaklik"),
                    // The legacy schema records no completion flag. A visit that happened is not
                    // distinguishable here from one that was only planned, so nothing is claimed.
                    false,
                    Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    false,
                    organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId)
                        ? tenantId
                        : (object?)null,
                ];
            }
        }

        if (context.DryRun)
        {
            // Nothing is written, but the rows are still produced, so the counts are real.
            await foreach (var _ in RowsAsync(cancellationToken))
            {
            }

            return (read, read - orphaned, orphaned, lastId);
        }

        var written = await context.Bulk.WriteAsync("ensa.Visit", Columns, RowsAsync(cancellationToken), cancellationToken);
        return (read, written, orphaned, lastId);
    }

    // ------------------------------------------------------------------ helpers

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

    private static string? Fit(MigrationContext context, string table, string column, string? value)
        => context.Fitter.Fit(table, column, value);

    private static string? Text(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index)?.ToString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int? Int(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToInt32(reader.GetValue(index));
    }

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    /// <summary>
    /// A distance in kilometres, kept inside what the column can hold.
    /// <para>
    /// The legacy column is a float that has been used for whatever was to hand; the destination is
    /// a decimal with a fixed precision, and one absurd value would stop the whole table.
    /// </para>
    /// </summary>
    private static decimal? Distance(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = Convert.ToDouble(reader.GetValue(index));
        return value is >= 0 and < 100_000 ? (decimal)value : null;
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));
}
