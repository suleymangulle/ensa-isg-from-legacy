using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// Supplies the company/user ceilings that follow from the plan the tenant (organization) has
/// purchased.
/// <para>
/// It is abstracted so that the domain layer does not depend directly on the
/// <c>Organization</c> (tenant) and <c>Package</c> entities; the implementation lives in the
/// <c>Ensa.EntityFrameworkCore</c> layer. Returning <c>null</c> means "no limit".
/// </para>
/// </summary>
public interface ITenantLimitProvider
{
    /// <summary>Maximum number of active companies the tenant may record. <c>null</c> = unlimited.</summary>
    Task<int?> GetCompanyLimitAsync(int? tenantId, CancellationToken cancellationToken = default);

    /// <summary>Maximum number of distance-learning users a company may define. <c>null</c> = unlimited.</summary>
    Task<int?> GetCompanyPerUserLimitAsync(int? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Supplies the statutory hazard class that belongs to a NACE (occupation) code definition.
/// <para>
/// It is abstracted to avoid a direct dependency on the host reference table
/// <c>OccupationCode</c>; the implementation lives in the <c>Ensa.EntityFrameworkCore</c> layer.
/// </para>
/// </summary>
public interface INaceHazardClassProvider
{
    /// <summary>
    /// The official hazard class of the occupation code. Returns <c>null</c> when the code is not
    /// found (in which case the consistency check is skipped).
    /// </summary>
    Task<HazardClass?> GetHazardClassAsync(int occupationCodeId, CancellationToken cancellationToken = default);
}
