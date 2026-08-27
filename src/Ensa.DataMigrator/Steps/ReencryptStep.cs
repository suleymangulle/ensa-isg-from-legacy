using Ensa.DataMigrator.Infrastructure;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Re-encrypts every encrypted column from one key to another.
/// <para>
/// <b>Why it had to exist.</b> <c>EnsaEncryptionOptions.Current</c> is a process-wide static that EF
/// model building reads, and it is set by <c>AddEnsaEntityFrameworkCore</c>. This tool builds its
/// DbContext by hand and never called that, so the converter fell back to the published development
/// key — and everything the migration wrote to an encrypted column went in under a key the
/// application does not use. 250,490 identity numbers.
/// </para>
/// <para>
/// <b>Nothing complained, and nothing would have.</b> The verification read the values back through
/// the migrator's own context, so it used the same wrong key and reported them as perfectly healthy.
/// It would have surfaced the first time somebody opened an employee in the application. What
/// exposed it was the bulk-copy guard refusing an encrypted column, which prompted the question of
/// where the migrator's key came from at all.
/// </para>
/// <para>
/// <b>Which columns.</b> Taken from the EF model — a property is encrypted when the converter is
/// attached to it — so this cannot fall behind the configuration. The same routine serves the key
/// rotation the domain documentation already said would need a separate data migration.
/// </para>
/// <para>
/// It is <b>idempotent by inspection</b>: a value that already decrypts under the destination key is
/// left alone. A run that dies halfway can simply be run again.
/// </para>
/// </summary>
public sealed class ReencryptStep : IMigrationStep
{
    public int Order => 9500;

    public string Name => "reencrypt";

    public string Description => "Rewrites every encrypted column from the development fallback key to the configured one";

    /// <summary>
    /// Rows per statement. Each carries two parameters and SQL Server accepts 2,100 in one request,
    /// so a thousand is the most that fits with room to spare.
    /// </summary>
    private const int BatchSize = 1000;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        var destination = EnsaEncryptionOptions.Current;

        if (!destination.IsConfigured)
        {
            return new StepResult(0, 0, 0,
                "no configured key, so there is nothing to move values to — skipped");
        }

        // The source: what the converter falls back to when nothing is configured. Empty options
        // resolve to the fixed development string, which is exactly what the broken runs used.
        var source = new EnsaEncryptionOptions();

        var columns = EncryptedColumns(context);
        var read = 0;
        var written = 0;
        var unreadable = 0;
        var notes = new List<string>();

        foreach (var (table, column) in columns)
        {
            var result = await RepairColumnAsync(context, table, column, source, destination, cancellationToken);

            read += result.Read;
            written += result.Written;
            unreadable += result.Skipped;

            if (result.Written > 0 || result.Skipped > 0)
            {
                notes.Add(result.Note!);
            }
        }

        if (notes.Count == 0)
        {
            notes.Add("every encrypted value already reads under the configured key");
        }

        return new StepResult(read, written, unreadable, string.Join("; ", notes));
    }

    /// <summary>Every table and column the model marks as encrypted.</summary>
    private static List<(string Table, string Column)> EncryptedColumns(MigrationContext context)
    {
        using var db = context.CreateDbContext();

        return db.Model.GetEntityTypes()
            .Where(entity => entity.GetTableName() is not null)
            .SelectMany(entity => entity.GetProperties()
                .Where(property => property.GetValueConverter() is EncryptedStringConverter)
                .Select(property => (Table: entity.GetTableName()!, Column: property.GetColumnName())))
            .Distinct()
            .OrderBy(pair => pair.Table)
            .ThenBy(pair => pair.Column)
            .ToList();
    }

    private static async Task<StepResult> RepairColumnAsync(
        MigrationContext context,
        string table,
        string column,
        EnsaEncryptionOptions source,
        EnsaEncryptionOptions destination,
        CancellationToken cancellationToken)
    {
        var sourceConverter = new EncryptedStringConverter(source);
        var destinationConverter = new EncryptedStringConverter(destination);

        var read = 0;
        var repaired = 0;
        var unreadable = 0;
        var pending = new List<(int Id, string Value)>();

        await using var connection = await context.OpenModernAsync(cancellationToken);

        // Raw SQL on purpose: the point is to see the stored ciphertext, which is precisely what
        // reading through the model would hide.
        await using (var command = new SqlCommand(
            $"SELECT Id, [{column}] FROM ensa.[{table}] WHERE [{column}] IS NOT NULL", connection)
            { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var id = reader.GetInt32(0);
                var stored = reader.GetString(1);

                // Already correct: decrypting under the destination key gives something back.
                if (TryDecrypt(destinationConverter, stored) is not null)
                {
                    continue;
                }

                var plaintext = TryDecrypt(sourceConverter, stored);
                if (plaintext is null)
                {
                    // Readable under neither key. Reported rather than overwritten: whatever it is,
                    // guessing would replace it with something wrong.
                    unreadable++;
                    continue;
                }

                pending.Add((id, (string)destinationConverter.ConvertToProvider(plaintext)!));
            }
        }

        foreach (var chunk in pending.Chunk(BatchSize))
        {
            await using var command = new SqlCommand(
                $"""
                 UPDATE target SET [{column}] = source.Value
                 FROM ensa.[{table}] AS target
                 JOIN (VALUES {string.Join(",", chunk.Select((_, index) => $"(@i{index},@v{index})"))})
                      AS source (Id, Value) ON target.Id = source.Id;
                 """, connection) { CommandTimeout = 1800 };

            for (var index = 0; index < chunk.Length; index++)
            {
                command.Parameters.AddWithValue($"@i{index}", chunk[index].Id);
                command.Parameters.AddWithValue($"@v{index}", chunk[index].Value);
            }

            repaired += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (repaired > 0 || unreadable > 0)
        {
            context.Logger.LogInformation(
                "    {Table}.{Column}: {Repaired} re-encrypted, {Unreadable} unreadable, of {Read}",
                table, column, repaired, unreadable, read);
        }

        var note = $"{table}.{column}: {repaired} re-encrypted";
        if (unreadable > 0)
        {
            note += $", {unreadable} UNREADABLE under either key";
        }

        return new StepResult(read, repaired, unreadable, note);
    }

    /// <summary>
    /// Decrypts, or returns null when the value does not belong to this key.
    /// <para>
    /// <b>The converter never throws.</b> <c>EncryptedStringConverter.Decrypt</c> catches its own
    /// failures and returns the input unchanged — deliberate tolerance, so a row that was never
    /// encrypted does not crash the application. A try/catch around it therefore always "succeeds",
    /// which is how the first version of this step concluded that all 254,873 values were already
    /// correct while the verification, looking at the shape of the result, correctly said none were.
    /// </para>
    /// <para>
    /// So the test is whether the output differs from the input. A genuine decryption changes the
    /// value; a failed one gives it straight back.
    /// </para>
    /// </summary>
    private static string? TryDecrypt(EncryptedStringConverter converter, string stored)
    {
        var value = converter.ConvertFromProvider(stored) as string;

        return string.IsNullOrEmpty(value) || string.Equals(value, stored, StringComparison.Ordinal)
            ? null
            : value;
    }
}
