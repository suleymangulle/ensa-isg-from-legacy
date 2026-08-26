using Ensa.Domain.Common;
using Ensa.Domain.Lookups;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Lookups;

/// <summary>
/// Queries specific to the <see cref="Parameter"/> module.
/// </summary>
public class ParameterRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<Parameter>(context, dataFilter), IParameterRepository
{
    /// <summary>
    /// Returns the parameter value for the given code, or <c>null</c> when there is no record.
    /// <para>
    /// <b>Precedence:</b> for the same code the global query filter may return both a tenant-specific row
    /// (<c>TenantId == CurrentTenantId</c>) and a shared one (<c>TenantId == null</c>). The tenant-specific
    /// value OVERRIDES the shared one, so the ordering puts the populated <c>TenantId</c> first and a single
    /// row is read. This is resolved in one query.
    /// </para>
    /// </summary>
    public Task<string?> GetValueAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult<string?>(null);
        }

        return GetReadOnlyQueryable()
            .Where(p => p.Code == code && p.IsActive)
            .OrderBy(p => p.TenantId == null ? 1 : 0)
            .ThenByDescending(p => p.Id)
            .Select(p => (string?)p.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
