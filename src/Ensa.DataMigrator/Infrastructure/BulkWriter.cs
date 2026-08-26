using System.Data;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Infrastructure;

/// <summary>
/// Streams rows straight into a destination table, for the tables that are too large for
/// Entity Framework.
/// <para>
/// <b>Why.</b> Writing through the DbContext costs about 340 rows a second across this connection —
/// fine for the 263,323 employees, and five hours for the six and a half million rows still to
/// come. <c>SqlBulkCopy</c> is the difference between a migration that runs during a coffee break
/// and one that runs overnight.
/// </para>
/// <para>
/// <b>When it is safe, and when it is not.</b> Bulk copy bypasses the model, so it also bypasses
/// the value converters. A table with an encrypted column must go through the DbContext or its
/// plaintext lands in a column everything else will try to decrypt. <see cref="EnsureNoConverters"/>
/// refuses to write such a table rather than trusting the caller to remember: the mistake is
/// silent, and this migration has already made it once.
/// </para>
/// <para>
/// It also bypasses the <c>SaveChanges</c> interceptor, so the audit columns and <c>TenantId</c>
/// have to be supplied by the caller. That is deliberate here — a migration sets them from the
/// legacy row rather than from the ambient context anyway.
/// </para>
/// </summary>
public sealed class BulkWriter(string connectionString)
{
    /// <summary>Rows per network round trip.</summary>
    private const int BatchSize = 5000;

    /// <summary>
    /// Refuses a table that has an encrypted column.
    /// <para>
    /// A guard rather than a comment, because the failure is invisible: the insert succeeds, the
    /// column looks like every other one, and the value is only wrong when somebody reads it back.
    /// </para>
    /// </summary>
    public static void EnsureNoConverters(DbContext db, string table)
    {
        var entityType = db.Model.GetEntityTypes()
            .FirstOrDefault(e => string.Equals(e.GetTableName(), table, StringComparison.OrdinalIgnoreCase));

        if (entityType is null)
        {
            return;
        }

        var encrypted = entityType.GetProperties()
            .Where(p => p.GetValueConverter() is EncryptedStringConverter)
            .Select(p => p.Name)
            .ToList();

        if (encrypted.Count > 0)
        {
            throw new InvalidOperationException(
                $"'{table}' has encrypted column(s) ({string.Join(", ", encrypted)}) and cannot be "
                + "written with bulk copy: the converter would be bypassed and the plaintext stored "
                + "in a column every reader will try to decrypt. Use the DbContext for this table.");
        }
    }

    /// <summary>
    /// Writes every row the reader yields into <paramref name="table"/>.
    /// </summary>
    /// <param name="table">Destination, schema-qualified.</param>
    /// <param name="columns">Destination column names, in the order the rows supply their values.</param>
    /// <param name="rows">The rows. Streamed, so the whole set is never held in memory.</param>
    /// <returns>How many rows were written.</returns>
    public async Task<int> WriteAsync(
        string table,
        IReadOnlyList<string> columns,
        IAsyncEnumerable<object?[]> rows,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var bulk = new SqlBulkCopy(connection)
        {
            DestinationTableName = table,
            BatchSize = BatchSize,
            BulkCopyTimeout = 0,
            EnableStreaming = true,
        };

        foreach (var column in columns)
        {
            bulk.ColumnMappings.Add(column, column);
        }

        var source = new StreamingReader(columns, rows, cancellationToken);
        await bulk.WriteToServerAsync(source, cancellationToken);

        return source.RowCount;
    }

    /// <summary>
    /// Adapts an <see cref="IAsyncEnumerable{T}"/> of rows to the <see cref="IDataReader"/> that
    /// <see cref="SqlBulkCopy"/> consumes, so nothing is buffered.
    /// <para>
    /// <c>SqlBulkCopy</c> pulls synchronously even in streaming mode, which is why the enumerator is
    /// advanced with <c>GetAwaiter().GetResult()</c> here. It is confined to this adapter: the rows
    /// come from a <c>SqlDataReader</c> that is already streaming, so nothing else blocks.
    /// </para>
    /// </summary>
    private sealed class StreamingReader(
        IReadOnlyList<string> columns,
        IAsyncEnumerable<object?[]> rows,
        CancellationToken cancellationToken) : IDataReader
    {
        private readonly IAsyncEnumerator<object?[]> _enumerator = rows.GetAsyncEnumerator(cancellationToken);
        private object?[] _current = [];

        public int RowCount { get; private set; }

        public bool Read()
        {
            if (!_enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                return false;
            }

            _current = _enumerator.Current;
            RowCount++;
            return true;
        }

        public int FieldCount => columns.Count;

        public object GetValue(int i) => _current[i] ?? DBNull.Value;

        public int GetOrdinal(string name)
        {
            for (var index = 0; index < columns.Count; index++)
            {
                if (string.Equals(columns[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            throw new IndexOutOfRangeException(name);
        }

        public string GetName(int i) => columns[i];

        public bool IsDBNull(int i) => _current[i] is null;

        public void Dispose() => _enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();

        // SqlBulkCopy uses only the members above. The rest of IDataReader is not reachable from it.
        public bool NextResult() => false;
        public int Depth => 0;
        public bool IsClosed => false;
        public int RecordsAffected => RowCount;
        public void Close() { }
        public DataTable? GetSchemaTable() => null;
        public bool GetBoolean(int i) => (bool)GetValue(i);
        public byte GetByte(int i) => (byte)GetValue(i);
        public long GetBytes(int i, long o, byte[]? buffer, int bufferOffset, int length)
            => throw new NotSupportedException();
        public char GetChar(int i) => (char)GetValue(i);
        public long GetChars(int i, long o, char[]? buffer, int bufferOffset, int length)
            => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();
        public string GetDataTypeName(int i) => GetValue(i).GetType().Name;
        public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
        public decimal GetDecimal(int i) => (decimal)GetValue(i);
        public double GetDouble(int i) => (double)GetValue(i);
        public Type GetFieldType(int i) => typeof(object);
        public float GetFloat(int i) => (float)GetValue(i);
        public Guid GetGuid(int i) => (Guid)GetValue(i);
        public short GetInt16(int i) => (short)GetValue(i);
        public int GetInt32(int i) => (int)GetValue(i);
        public long GetInt64(int i) => (long)GetValue(i);
        public string GetString(int i) => (string)GetValue(i);
        public int GetValues(object[] values) => throw new NotSupportedException();
        public object this[int i] => GetValue(i);
        public object this[string name] => GetValue(GetOrdinal(name));
    }
}
