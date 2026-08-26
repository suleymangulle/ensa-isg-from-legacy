using Ensa.DataMigrator.Infrastructure;
using Microsoft.Data.SqlClient;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Reconciles what was migrated against what it came from, row by row.
/// <para>
/// <b>Why this is a step and not a one-off query.</b> A migration that reports "29,024 written" has
/// proved that 29,024 inserts succeeded, which is not the same as proving the right values landed.
/// The interesting failures are silent: a name truncated by a narrower column, a Turkish character
/// lost to a codepage, a foreign key pointing at the wrong parent because two rows matched on a
/// name. None of those raise an error.
/// </para>
/// <para>
/// Both databases sit on the same server, so the check can join across them through the id map and
/// compare the actual values. Comparison forces a <b>binary</b> collation: the databases are
/// <c>Turkish_CI_AS</c>, under which "Ş" and "ş" are equal, and a check that cannot see the
/// difference between them cannot see a case-mangling bug either.
/// </para>
/// <para>
/// It never writes, so it is safe to run at any time, and it runs last.
/// </para>
/// </summary>
public sealed class VerifyStep : IMigrationStep
{
    public int Order => 9000;

    public string Name => "verify";

    public string Description => "Compares migrated values against the legacy rows, byte for byte";

    /// <summary>
    /// One comparison: a legacy table and column against the modern table and column it became.
    /// </summary>
    private sealed record Check(
        string LegacyTable,
        string LegacyKey,
        string LegacyValue,
        string ModernTable,
        string ModernValue);

    private static readonly Check[] Checks =
    [
        new("Mahalle_T", "MahalleId", "MahalleAdi", "ensa.Neighborhood", "NeighborhoodName"),
        new("Ilce_T", "IlceId", "IlceAdi", "ensa.District", "DistrictName"),
        new("Sehir_T", "SehirId", "SehirAdi", "ensa.City", "CityName"),
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        var compared = 0;
        var identical = 0;
        var different = 0;
        var notes = new List<string>();

        await using var connection = await context.OpenModernAsync(cancellationToken);

        foreach (var check in Checks)
        {
            // LTRIM/RTRIM on the legacy side strips spaces but NOT the CR/LF that has been found
            // embedded in a few names; the migration's .NET Trim() removes those. Such a row is
            // reported separately, because it is the migration cleaning data rather than losing it.
            var sql = $"""
                WITH pair AS (
                    SELECT CONVERT(nvarchar(400), LTRIM(RTRIM(l.{check.LegacyValue}))) AS legacyValue,
                           n.{check.ModernValue}                                        AS modernValue
                    FROM migration.IdMap m
                    JOIN {context.Target.LegacyDatabase}.dbo.{check.LegacyTable} l
                      ON l.{check.LegacyKey} = m.LegacyId
                    JOIN {check.ModernTable} n
                      ON n.Id = m.ModernId
                    WHERE m.LegacyTable = @table
                      -- Only the rows this migration INSERTED. A matched row took the seeded
                      -- catalogue's value on purpose - "Hakkari" in the legacy data is the same
                      -- province as "Hakkari" with a circumflex in the catalogue - so holding it to
                      -- byte-equality would report 894 correct rows as failures, and a check that
                      -- cries wolf is a check nobody reads.
                      AND m.Resolution = 'I'
                )
                SELECT COUNT(*),
                       SUM(CASE WHEN legacyValue COLLATE Latin1_General_BIN2
                                   = modernValue COLLATE Latin1_General_BIN2
                                THEN 1 ELSE 0 END),
                       SUM(CASE WHEN legacyValue COLLATE Latin1_General_BIN2
                                   <> modernValue COLLATE Latin1_General_BIN2
                                AND REPLACE(REPLACE(legacyValue, CHAR(13), ''), CHAR(10), '')
                                       COLLATE Latin1_General_BIN2
                                  = modernValue COLLATE Latin1_General_BIN2
                                THEN 1 ELSE 0 END)
                FROM pair;
                """;

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 600 };
            command.Parameters.AddWithValue("@table", check.LegacyTable);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0))
            {
                continue;
            }

            var total = reader.GetInt32(0);
            var same = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var whitespaceOnly = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            var mismatched = total - same - whitespaceOnly;

            compared += total;
            identical += same;
            different += mismatched;

            var note = $"{check.LegacyTable}: {same}/{total} inserted rows identical";
            if (whitespaceOnly > 0)
            {
                note += $", {whitespaceOnly} trimmed of stray CR/LF";
            }

            if (mismatched > 0)
            {
                note += $", {mismatched} MISMATCHED";
            }

            notes.Add(note);
        }

        if (different > 0)
        {
            throw new InvalidOperationException(
                $"Verification failed: {different} migrated value(s) differ from the legacy row "
                + $"they came from. {string.Join("; ", notes)}");
        }

        return new StepResult(compared, 0, 0, string.Join("; ", notes));
    }
}
