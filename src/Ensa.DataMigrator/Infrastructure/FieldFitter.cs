using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Infrastructure;

/// <summary>
/// Fits a legacy value into the destination column, and keeps count of every time it had to.
/// <para>
/// <b>Why this exists.</b> A decade of free-text entry does not respect the field it was typed
/// into. The first real run of the tenancy step died on an organization whose "authorised person's
/// telephone" holds <c>orhan.soylu58@gmail.com</c> — longer than the phone column, and not a phone
/// number at all. There will be more of them, and one such row must not stop the other 1,039.
/// </para>
/// <para>
/// The three ways to handle it are: fail (one bad row blocks the migration), truncate silently
/// (quiet data loss, discovered by a user months later), or truncate and say so. This is the third.
/// Every shortened value is counted per column and reported in the step's summary, so an
/// over-long field is a number somebody can look at rather than an accident.
/// </para>
/// <para>
/// Lengths come from the destination's own <c>sys.columns</c>, not from constants: a limit copied
/// into the migrator is a limit that drifts away from the schema it is supposed to respect.
/// </para>
/// </summary>
public sealed class FieldFitter
{
    private readonly Dictionary<string, int> _maxLengths = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _truncations = new(StringComparer.Ordinal);

    /// <summary>Reads the character limit of every text column in the destination schema.</summary>
    public async Task LoadAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT t.name, c.name,
                   CASE WHEN c.max_length = -1 THEN 2147483647
                        WHEN ty.name IN ('nvarchar', 'nchar') THEN c.max_length / 2
                        ELSE c.max_length END
            FROM sys.tables t
            JOIN sys.columns c ON c.object_id = t.object_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE SCHEMA_NAME(t.schema_id) = 'ensa'
              AND ty.name IN ('nvarchar', 'nchar', 'varchar', 'char');
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            _maxLengths[$"{reader.GetString(0)}.{reader.GetString(1)}"] = reader.GetInt32(2);
        }
    }

    /// <summary>
    /// Replaces the limit of every encrypted column with the length its <b>plaintext</b> may be.
    /// <para>
    /// An encrypted column is sized to hold ciphertext: <c>NationalId</c> is <c>nvarchar(64)</c>
    /// because that is what eleven characters become after AES and Base64. Fitting the plaintext to
    /// 64 lets a twenty-character legacy value through, which then encrypts to more than the column
    /// holds. The limit that matters is the one before encryption.
    /// </para>
    /// <para>
    /// Which properties are encrypted comes from the model itself - the converter is attached to
    /// them - so this cannot drift away from the configuration.
    /// </para>
    /// </summary>
    public void ApplyEncryptedColumnLimits(DbContext db)
    {
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (table is null)
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.GetValueConverter() is not EncryptedStringConverter)
                {
                    continue;
                }

                var columnLength = property.GetMaxLength();
                if (columnLength is not { } physical)
                {
                    continue;
                }

                _maxLengths[$"{table}.{property.GetColumnName()}"] = PlainCapacity(physical);
            }
        }
    }

    /// <summary>
    /// The longest plaintext whose encrypted form still fits in <paramref name="columnLength"/>.
    /// <para>
    /// Found by walking the same function the schema was built with rather than re-deriving the
    /// arithmetic, so a change to the encryption format cannot leave this behind.
    /// </para>
    /// </summary>
    private static int PlainCapacity(int columnLength)
    {
        var plain = 0;
        while (EncryptedStringConverter.EncryptedMaxLength(plain + 1) <= columnLength)
        {
            plain++;
        }

        return plain;
    }

    /// <summary>
    /// Returns the value shortened to what <paramref name="table"/>.<paramref name="column"/> can
    /// hold, recording the fact when it did not fit.
    /// </summary>
    public string? Fit(string table, string column, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var key = $"{table}.{column}";

        // A column the destination does not have is a mapping mistake, not a length problem, and
        // silently letting the value through would hide it until the INSERT fails.
        if (!_maxLengths.TryGetValue(key, out var maxLength) || value.Length <= maxLength)
        {
            return value;
        }

        _truncations[key] = _truncations.GetValueOrDefault(key) + 1;
        return value[..maxLength];
    }

    /// <summary>Whether anything had to be shortened.</summary>
    public bool HasTruncations => _truncations.Count > 0;

    /// <summary>One line naming each column that lost characters, and how many rows.</summary>
    public string Report()
        => string.Join(", ", _truncations
            .OrderByDescending(entry => entry.Value)
            .Select(entry => $"{entry.Key} x{entry.Value}"));

    /// <summary>Forgets the counts, so each step reports only its own.</summary>
    public void Reset() => _truncations.Clear();
}
