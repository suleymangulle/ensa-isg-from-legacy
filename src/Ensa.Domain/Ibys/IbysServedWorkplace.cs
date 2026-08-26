using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// A "served workplace" record the OHS service provider reports to IBYS, together with the
/// service start/end period.
/// <para>Legacy equivalent: <c>IBYSServiceProvidedWorkplace_T</c>.</para>
/// <para>Tenant-scoped transactional record (legacy <c>OrganizationId</c> → base <c>TenantId</c>).</para>
/// </summary>
public class IbysServedWorkplace : FullAuditedTenantEntity, IActivatable, ICompanyScoped
{
    /// <summary>The workplace being served. (Legacy: <c>FirmaId</c>)</summary>
    public int CompanyId { get; set; }

    /// <summary>User who approved the submission with an e-signature. (Legacy: <c>OnaylayanKullanici</c>)</summary>
    public int ApproverUserId { get; set; }

    /// <summary>Service start date. (Legacy: <c>HizmetBaslangicTarihi</c>)</summary>
    public DateTime ServiceStartDate { get; set; }

    /// <summary>Service end date; <c>null</c> while the contract is still running. (Legacy: <c>HizmetBitisTarihi</c>)</summary>
    public DateTime? ServiceEndDate { get; set; }

    /// <summary>Submission number returned by IBYS. ENCRYPTED COLUMN. (Legacy: <c>IBYSBildirimNo</c>)</summary>
    public string? IbysNotificationNo { get; set; }

    /// <summary>
    /// Raw XML that was sent. LONG TEXT (<c>nvarchar(max)</c>) + ENCRYPTED COLUMN;
    /// the converter will be wired up in phase 2. (Legacy: <c>XmlVeri</c>)
    /// </summary>
    public string? XmlData { get; set; }

    /// <summary>
    /// E-signed payload. LONG TEXT (<c>nvarchar(max)</c>) + ENCRYPTED COLUMN;
    /// the converter will be wired up in phase 2. (Legacy: <c>ImzaliVeri</c>)
    /// </summary>
    public string? SignedData { get; set; }

    /// <summary>Whether the submission is active. (Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
