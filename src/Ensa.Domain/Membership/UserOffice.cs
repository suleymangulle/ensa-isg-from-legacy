using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// User-to-office assignment (many-to-many). A user can work in several offices, each with a
/// different monthly duration.
/// Legacy: <c>KullaniciOfis_T</c> (PK <c>UserOfficeId</c>, tenant column <c>OrganizationId</c>).
/// <para>Join table → <see cref="CreationAuditedTenantEntity"/>.</para>
/// </summary>
public class UserOffice : CreationAuditedTenantEntity
{
    public int UserId { get; set; }

    public int OfficeId { get; set; }

    /// <summary>Monthly working time committed to this office, in minutes. (Legacy: Sure int)</summary>
    public int MonthlyWorkDurationMinutes { get; set; }
}
