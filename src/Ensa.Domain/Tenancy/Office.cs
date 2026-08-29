using Ensa.Domain.Common;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// A physical office or branch of the organization. Users and work plans are managed per office.
/// Legacy: <c>Ofisler_T</c> (PK <c>OfficeId</c>, tenant column <c>OrganizationId</c>).
///
/// <para>
/// <b>An office belongs to the organization, not to a customer workplace</b>, so it is tenant-scoped
/// and deliberately <b>not</b> <see cref="ICompanyScoped"/>. It used to be, and that was wrong in a
/// way that was easy to miss: the company-scope filter fails closed, treating a row whose
/// <see cref="CompanyId"/> is <c>null</c> as provider-level data and hiding it from anyone bound to
/// a workplace. Every office in the database has a null <see cref="CompanyId"/>, so marking the
/// entity company-scoped hid <i>every</i> office from any company-bound user — including the offices
/// they were themselves assigned to, which left the shell unable to name the office it was working
/// in. Tenant isolation, active state and soft delete are unchanged and still carry the whole
/// boundary.
/// </para>
/// </summary>
public class Office : FullAuditedTenantEntity, IActivatable
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
    /// FK to the company the office is attributed to. (Legacy: <c>COFirmaId</c> — statistics filtered
    /// on it as <c>o.COCompanyId == companyId</c>.) <c>null</c> means the office reports directly to
    /// the organization, which is the case for every office in the migrated data.
    /// <para>
    /// This is an <b>attribution</b>, not a scope key: it is an optional filter on the office
    /// administration list and nothing else reads it. The column is kept because the legacy data has
    /// somewhere to land and because a filter is exposed over it; it must not be used to decide who
    /// may see an office. See the type's own remarks for why the entity is not
    /// <see cref="ICompanyScoped"/>.
    /// </para>
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>Whether this is the organization's headquarter office. Only one record per organization may be <c>true</c>.</summary>
    public bool IsHeadquarterOffice { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
