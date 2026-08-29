using Ensa.Domain.Repositories;
using Ensa.Domain.Tenancy.Navigations;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// Module-specific repository contract for <see cref="Office"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IOfficeRepository : IRepository<Office>
{
    /// <summary>The active tenant's headquarter office (<c>HeadquarterOffice == true</c>).</summary>
    Task<Office?> FindHeadquarterOfficeAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the office together with its organization and location details.</summary>
    Task<OfficeNavigation?> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Every office a user is assigned to through <c>UserOffice</c>.</summary>
    Task<List<Office>> GetUserOfficesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The office id of the user's <b>first</b> <c>UserOffice</c> assignment, by row id.
    /// <para>
    /// For a migrated account that row is their legacy default office: the data migration writes
    /// <c>Kullanici_T.OfisId</c> as an assignment in <c>TenancyStep</c> before <c>UserSplitStep</c>
    /// adds the <c>KullaniciOfis_T</c> rows, so the lowest id is the legacy default. For an account
    /// created since, it is simply the assignment that was made first. Ordered explicitly, so the
    /// answer does not depend on how the database happens to return rows.
    /// </para>
    /// </summary>
    Task<int?> FindDefaultUserOfficeIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>The offices belonging to the given company. (Legacy: the <c>Ofisler_T.COFirmaId</c> filter)</summary>
    Task<List<Office>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default);
}
