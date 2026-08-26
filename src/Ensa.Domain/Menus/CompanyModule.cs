using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// A module enabled for a company — per-company module entitlement.
/// Legacy: <c>FirmaModulBaglanti_T</c>.
/// <para>
/// The legacy table had NO <c>OrganizationId</c> column. Since <c>CompanyId</c> already points at a
/// tenant-owned record, the link itself was brought into tenant scope as well, which prevents data
/// leaking across tenants.
/// </para>
/// </summary>
public class CompanyModule : AuditedTenantEntity, IActivatable, ICompanyScoped
{
    public int CompanyId { get; set; }

    public int ModuleId { get; set; }

    /// <summary>Whether the module is enabled for the company. (Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
