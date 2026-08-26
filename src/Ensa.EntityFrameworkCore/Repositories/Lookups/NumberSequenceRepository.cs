using System.Data;
using System.Data.Common;
using Ensa.Domain.Common;
using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ensa.EntityFrameworkCore.Repositories.Lookups;

/// <summary>
/// Queries specific to the <see cref="NumberSequence"/> module (document number counter).
/// </summary>
public class NumberSequenceRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<NumberSequence>(context, dataFilter), INumberSequenceRepository
{
    /// <summary>
    /// Qualified name of the counter table. <c>NumberSequenceConfiguration</c> maps the table to
    /// <c>ensa.NumberSequence</c> and renames no column; the raw SQL below relies on that mapping.
    /// </summary>
    private const string TableName = "[" + EnsaDomainSharedConsts.DbSchema + "].[NumberSequence]";

    /// <summary>
    /// Increments the counter <b>in a single atomic statement</b> and reads the new value.
    /// <para>
    /// <b>WHY RAW SQL?</b> The classic "read → increment → write" flow hands the same number to two
    /// concurrent requests (lost update). With EF Core the only safe alternatives are optimistic
    /// concurrency plus a retry loop, or a row lock. Here SQL Server's <c>UPDATE ... OUTPUT INSERTED</c>
    /// pattern is used: the increment and the read happen in the same statement under the same row lock,
    /// so two concurrent requests queue up and receive different numbers.
    /// </para>
    /// <para>
    /// <b>When the row does not exist:</b> the <c>UPDLOCK, HOLDLOCK</c> hint makes SQL Server take a
    /// <i>key-range lock</i> on the unique index (<c>TenantId, ScopeId, Type</c>) when no matching row is
    /// found. That prevents another session from inserting the same key during the "insert if missing" step
    /// and stops duplicate counter rows from appearing. The statement is wrapped in an explicit transaction
    /// so that both steps stay within the same lock scope.
    /// </para>
    /// <para>
    /// <b>Tenant:</b> because raw SQL does not go through the global query filters, the tenant predicate has
    /// to be written <b>by hand</b> here; the value is taken from
    /// <see cref="EnsaDbContext.CurrentTenantId"/> and passed as a parameter (never concatenated into SQL).
    /// </para>
    /// </summary>
    public virtual async Task<int> GetNextNumberAsync(
        int scopeId,
        string type,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new BusinessException("The sequence type cannot be empty.", "Ensa:NumberSequenceTypeEmpty");
        }

        var sql = $"""
            SET NOCOUNT ON;

            DECLARE @result TABLE (Number int NOT NULL);

            BEGIN TRANSACTION;

            UPDATE n
               SET n.[LatestNumber] = n.[LatestNumber] + 1
            OUTPUT INSERTED.[LatestNumber] INTO @result (Number)
              FROM {TableName} AS n WITH (UPDLOCK, HOLDLOCK)
             WHERE n.[ScopeId] = @scopeId
               AND n.[Type] = @type
               AND ((@tenantId IS NULL AND n.[TenantId] IS NULL) OR n.[TenantId] = @tenantId);

            IF NOT EXISTS (SELECT 1 FROM @result)
            BEGIN
                INSERT INTO {TableName} ([TenantId], [ScopeId], [Type], [LatestNumber], [IsActive], [CreationTime])
                OUTPUT INSERTED.[LatestNumber] INTO @result (Number)
                VALUES (@tenantId, @scopeId, @type, 1, 1, SYSDATETIME());
            END

            COMMIT TRANSACTION;

            SELECT TOP (1) Number FROM @result;
            """;

        var connection = Context.Database.GetDbConnection();
        var weOpenedTheConnection = connection.State != ConnectionState.Open;

        if (weOpenedTheConnection)
        {
            await Context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = Context.Database.CurrentTransaction?.GetDbTransaction();

            command.Parameters.Add(Parameter(command, "@scopeId", DbType.Int32, scopeId));
            command.Parameters.Add(Parameter(command, "@type", DbType.String, type));
            command.Parameters.Add(Parameter(command, "@tenantId", DbType.Int32, Context.CurrentTenantId));

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null or DBNull)
            {
                throw new BusinessException(
                    $"The next number could not be generated for type '{type}' (ScopeId: {scopeId}).",
                    "Ensa:NumberSequenceGenerationFailed");
            }

            return Convert.ToInt32(result);
        }
        finally
        {
            if (weOpenedTheConnection)
            {
                await Context.Database.CloseConnectionAsync();
            }
        }
    }

    /// <summary>Creates a named SQL parameter (values are never concatenated into SQL).</summary>
    private static DbParameter Parameter(DbCommand command, string name, DbType tip, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = tip;
        parameter.Value = value ?? DBNull.Value;
        return parameter;
    }
}
