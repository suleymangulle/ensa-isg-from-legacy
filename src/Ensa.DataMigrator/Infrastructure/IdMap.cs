using Microsoft.Data.SqlClient;

namespace Ensa.DataMigrator.Infrastructure;

/// <summary>
/// Remembers which legacy row became which modern row.
/// <para>
/// <b>Why it exists.</b> Two reasons, and both are load-bearing.
/// </para>
/// <para>
/// <b>Foreign keys.</b> The legacy identities are not preserved — the modern tables have their own
/// identity columns, and several legacy tables collapse into one modern table (or split into
/// several). So <c>FirmaPersonel_T.FirmaId = 4711</c> cannot be copied across; it has to be
/// translated into whatever <c>Company</c> id that legacy company became. Every step writes its
/// translations here and later steps read them.
/// </para>
/// <para>
/// <b>Re-running.</b> A migration of this size is not a single command that either works or does
/// not: it is run, inspected, corrected and run again. With the map, a second run recognises the
/// rows it already created and updates them instead of inserting twins. Without it, the only safe
/// re-run is one that starts by emptying the destination.
/// </para>
/// <para>
/// It lives in its own <c>migration</c> schema, outside <c>EnsaDbContext</c>'s model. It is
/// scaffolding for a one-off exercise, not part of the product, and the model contract tests would
/// rightly complain about an entity nothing maps.
/// </para>
/// </summary>
public sealed class IdMap(string connectionString)
{
    private readonly Dictionary<string, Dictionary<int, int>> _cache = new(StringComparer.Ordinal);

    /// <summary>Creates the schema and table when they are not there yet.</summary>
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF SCHEMA_ID('migration') IS NULL EXEC('CREATE SCHEMA migration');

            IF OBJECT_ID('migration.IdMap') IS NULL
            CREATE TABLE migration.IdMap
            (
                LegacyTable  varchar(64) NOT NULL,
                LegacyId     int         NOT NULL,
                ModernId     int         NOT NULL,
                -- 'I' the migration inserted this row, so its value must match the legacy row
                -- byte for byte. 'M' it matched a row that was already there (the seed), whose
                -- value legitimately differs. The verification applies a different rule to each.
                Resolution   char(1)     NOT NULL CONSTRAINT DF_IdMap_Resolution DEFAULT 'I',
                MappedTime   datetime2   NOT NULL CONSTRAINT DF_IdMap_MappedTime DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_IdMap PRIMARY KEY (LegacyTable, LegacyId)
            );

            -- The table may predate the Resolution column: this tool's own schema evolves while
            -- the migration is being arrived at, and an existing map is worth keeping.
            IF COL_LENGTH('migration.IdMap', 'Resolution') IS NULL
            ALTER TABLE migration.IdMap
                ADD Resolution char(1) NOT NULL CONSTRAINT DF_IdMap_Resolution DEFAULT 'I';

            IF OBJECT_ID('migration.Watermark') IS NULL
            CREATE TABLE migration.Watermark
            (
                -- For a leaf table written in bulk: how far through the legacy table this has got.
                -- Reading in id order, a resumed run continues from here. A leaf needs no id map,
                -- and building one would mean reading back a million identities nobody looks up.
                LegacyTable  varchar(64) NOT NULL,
                LastLegacyId int         NOT NULL,
                UpdatedTime  datetime2   NOT NULL CONSTRAINT DF_Watermark_Updated DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_Watermark PRIMARY KEY (LegacyTable)
            );

            IF OBJECT_ID('migration.StepLog') IS NULL
            CREATE TABLE migration.StepLog
            (
                StepName     varchar(128) NOT NULL,
                StartedTime  datetime2    NOT NULL,
                FinishedTime datetime2    NULL,
                ReadCount    int          NOT NULL CONSTRAINT DF_StepLog_Read DEFAULT 0,
                WriteCount   int          NOT NULL CONSTRAINT DF_StepLog_Write DEFAULT 0,
                SkipCount    int          NOT NULL CONSTRAINT DF_StepLog_Skip DEFAULT 0,
                Note         nvarchar(max) NULL,
                CONSTRAINT PK_StepLog PRIMARY KEY (StepName, StartedTime)
            );
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Loads one legacy table's translations into memory for the rest of the run.</summary>
    public async Task<Dictionary<int, int>> LoadAsync(
        string legacyTable,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(legacyTable, out var cached))
        {
            return cached;
        }

        var map = new Dictionary<int, int>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT LegacyId, ModernId FROM migration.IdMap WHERE LegacyTable = @t", connection);
        command.Parameters.AddWithValue("@t", legacyTable);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            map[reader.GetInt32(0)] = reader.GetInt32(1);
        }

        _cache[legacyTable] = map;
        return map;
    }

    /// <summary>
    /// Writes a batch of translations, replacing any that are already there.
    /// </summary>
    /// <param name="resolution">
    /// <c>'I'</c> when the migration inserted these rows, <c>'M'</c> when it matched rows that
    /// already existed. The verification needs to know which, because only the first kind must be
    /// byte-identical to its legacy row.
    /// </param>
    public async Task SaveAsync(
        string legacyTable,
        IReadOnlyCollection<(int LegacyId, int ModernId)> pairs,
        char resolution = 'I',
        CancellationToken cancellationToken = default)
    {
        if (pairs.Count == 0)
        {
            return;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // A table-valued MERGE keeps this to one round trip per batch instead of one per row.
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        foreach (var chunk in pairs.Chunk(1000))
        {
            var values = string.Join(",", chunk.Select((_, index) => $"(@t,@l{index},@m{index},@r)"));

            await using var command = new SqlCommand($"""
                MERGE migration.IdMap AS target
                USING (VALUES {values}) AS source (LegacyTable, LegacyId, ModernId, Resolution)
                    ON target.LegacyTable = source.LegacyTable AND target.LegacyId = source.LegacyId
                WHEN MATCHED THEN UPDATE SET
                     ModernId = source.ModernId,
                     -- Sticky: a row this migration once inserted stays 'I' even when a later run
                     -- finds it already there and would otherwise call it a match. The flag is the
                     -- row's origin, not the outcome of the most recent pass, and the verification
                     -- must keep byte-checking everything the migration authored.
                     Resolution = CASE WHEN target.Resolution = 'I' THEN 'I' ELSE source.Resolution END
                WHEN NOT MATCHED THEN INSERT (LegacyTable, LegacyId, ModernId, Resolution)
                     VALUES (source.LegacyTable, source.LegacyId, source.ModernId, source.Resolution);
                """, connection, transaction);

            command.Parameters.AddWithValue("@t", legacyTable);
            command.Parameters.AddWithValue("@r", resolution);
            for (var index = 0; index < chunk.Length; index++)
            {
                command.Parameters.AddWithValue($"@l{index}", chunk[index].LegacyId);
                command.Parameters.AddWithValue($"@m{index}", chunk[index].ModernId);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        if (_cache.TryGetValue(legacyTable, out var cached))
        {
            foreach (var (legacyId, modernId) in pairs)
            {
                cached[legacyId] = modernId;
            }
        }
    }

    /// <summary>How far a bulk-written table has got, or zero when it has not started.</summary>
    public async Task<int> GetWatermarkAsync(string legacyTable, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT LastLegacyId FROM migration.Watermark WHERE LegacyTable = @t", connection);
        command.Parameters.AddWithValue("@t", legacyTable);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is int id ? id : 0;
    }

    /// <summary>
    /// Moves the watermark forward.
    /// <para>
    /// Written after each batch, not at the end: a run that dies halfway must leave the mark where
    /// the data actually stops, or the next run either repeats rows or skips them.
    /// </para>
    /// </summary>
    public async Task SetWatermarkAsync(
        string legacyTable, int lastLegacyId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("""
            MERGE migration.Watermark AS target
            USING (VALUES (@t, @id)) AS source (LegacyTable, LastLegacyId)
                ON target.LegacyTable = source.LegacyTable
            WHEN MATCHED THEN UPDATE SET LastLegacyId = source.LastLegacyId,
                                         UpdatedTime = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT (LegacyTable, LastLegacyId)
                 VALUES (source.LegacyTable, source.LastLegacyId);
            """, connection);

        command.Parameters.AddWithValue("@t", legacyTable);
        command.Parameters.AddWithValue("@id", lastLegacyId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Records that a step ran, and what it did.</summary>
    public async Task LogStepAsync(
        string stepName, DateTime startedTime, int read, int written, int skipped, string? note,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("""
            INSERT INTO migration.StepLog
                (StepName, StartedTime, FinishedTime, ReadCount, WriteCount, SkipCount, Note)
            VALUES (@name, @started, SYSUTCDATETIME(), @read, @write, @skip, @note);
            """, connection);

        command.Parameters.AddWithValue("@name", stepName);
        command.Parameters.AddWithValue("@started", startedTime);
        command.Parameters.AddWithValue("@read", read);
        command.Parameters.AddWithValue("@write", written);
        command.Parameters.AddWithValue("@skip", skipped);
        command.Parameters.AddWithValue("@note", (object?)note ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
