using Ensa.Domain.Common;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// A physical office or branch of the organization. Users and work plans are managed per office.
/// Legacy: <c>Ofisler_T</c> (PK <c>OfficeId</c>, tenant column <c>OrganizationId</c>).
/// </summary>
public class Office : FullAuditedTenantEntity, IActivatable, ICompanyScoped
{
    /// <summary>Office name. (Legacy: OfisAdi)</summary>
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Fax { get; set; }

    public string? Address { get; set; }

    public int? CityId { get; set; }

    /// <summary>Not present in legacy; added to normalize the address.</summary>
    public int? DistrictId { get; set; }

    public string? AuthorizedPerson { get; set; }

    /// <summary>(Legacy: YetkiliKisiEmail)</summary>
    public string? AuthorizedEmail { get; set; }

    /// <summary>
    /// FK to the company the office belongs to. (Legacy: <c>COFirmaId</c> — statistics filtered on it
    /// as <c>o.COCompanyId == companyId</c>.) <c>null</c> means the office reports directly to the
    /// organization.
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>Whether this is the organization's headquarter office. Only one record per organization may be <c>true</c>.</summary>
    public bool IsHeadquarterOffice { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
