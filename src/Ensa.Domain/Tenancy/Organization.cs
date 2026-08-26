using Ensa.Domain.Common;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// The tenant root record — the OHS service provider (OSGB) or enterprise using the system.
/// <para>
/// NEW ENTITY. Legacy had NO separate <c>Organization_T</c> table: although <c>OrganizationId</c>
/// appeared as an FK on every table, the organization's own data lived in the <c>Company_T</c> row
/// where <c>CompanyId == OrganizationId</c> (see the legacy <c>BaseController.Organization</c> →
/// <c>Company_T</c>). That ambiguity is resolved here; the tenant definition now has its own table.
/// </para>
/// <para>
/// CAUTION: this entity is the <b>definition</b> of a tenant, not a tenant-owned record. It
/// therefore does NOT implement <see cref="IMultiTenant"/> and is a host table with no
/// <c>TenantId</c>. The <c>TenantId</c> on every other entity refers to this table's <c>Id</c>.
/// </para>
/// </summary>
public class Organization : FullAuditedEntity, IActivatable
{
    /// <summary>Registered trading name of the organization.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique organization code, used for sub-domain and tenant resolution.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>FK to the organization type. (Legacy: the <c>Firma_T.KurumTuru</c> string code)</summary>
    public int OrganizationTypeId { get; set; }

    /// <summary>Abonelik paketi FK. (Legacy: Firma_T.PaketTuru string kodu)</summary>
    public int SubscriptionPlanId { get; set; }

    public string? TaxTaxOffice { get; set; }

    public string? TaxNumber { get; set; }

    public string? Address { get; set; }

    public int? CityId { get; set; }

    public int? DistrictId { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? WebUrl { get; set; }

    public string? AuthorizedFullName { get; set; }

    public string? AuthorizedPhone { get; set; }

    public string? AuthorizedEmail { get; set; }

    /// <summary>Organization logo — FK to the central <c>Document</c> table. The content is not held on the entity.</summary>
    public int? LogoDocumentId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime SubscriptionStart { get; set; }

    /// <summary><c>null</c> means the subscription never expires.</summary>
    public DateTime? SubscriptionEnd { get; set; }

    /// <summary>Plan quota — <c>null</c> means unlimited.</summary>
    public int? MaximumUserCount { get; set; }

    /// <summary>Plan quota — <c>null</c> means unlimited.</summary>
    public int? MaximumCompanyCount { get; set; }
}
