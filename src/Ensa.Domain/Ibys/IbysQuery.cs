using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Ibys;

/// <summary>
/// A submission/query record sent to IBYS (the national OHS information management system).
/// <para>Legacy equivalent: <c>IBYSQueryNo_T</c> (file <c>IBYSQuery_T.cs</c>), PK <c>QueryNoId</c> → <c>Id</c>.</para>
///
/// <para>
/// <b>MAGIC VALUES CONVERTED TO ENUMS:</b>
/// legacy <c>IBYSStatus</c> (<c>int?</c>) → <see cref="Status"/> (<see cref="IbysSubmissionStatus"/>),
/// legacy <c>QueryType</c> (<c>string</c>) → <see cref="QueryType"/> (<see cref="IbysQueryType"/>).
/// The raw code returned by the IBYS service itself is preserved in <see cref="StatusCode"/>.
/// </para>
///
/// <para>
/// <b>TENANT:</b> the legacy table had NO <c>OrganizationId</c> column; since the record is
/// transactional (tenant-scoped) data it derives from <c>FullAuditedTenantEntity</c> here.
/// During migration the <c>TenantId</c> of the related company is assigned through
/// <see cref="CompanyId"/>.
/// </para>
/// </summary>
public class IbysQuery : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>Query number returned by IBYS. (Legacy: <c>SorguNo</c>)</summary>
    public string? QueryNo { get; set; }

    /// <summary>Type of the submission. (Legacy: <c>SorguTur</c> string)</summary>
    public IbysQueryType QueryType { get; set; } = IbysQueryType.Unspecified;

    /// <summary>Submission/workflow status. (Legacy: <c>IBYSDurum</c> int?)</summary>
    public IbysSubmissionStatus Status { get; set; } = IbysSubmissionStatus.NotSent;

    /// <summary>Raw status code returned by the IBYS service. (Legacy: <c>DurumKodu</c>)</summary>
    public int StatusCode { get; set; }

    /// <summary>Message returned by the IBYS service. (Legacy: <c>IbysMesaji</c>)</summary>
    public string? IbysMessage { get; set; }

    /// <summary>Time of submission. (Legacy: <c>GonderimTarihi</c>)</summary>
    public DateTime SubmissionDate { get; set; }

    /// <summary>Batch identifier for bulk submissions. (Legacy: <c>GrupId</c>)</summary>
    public string? GroupId { get; set; }

    /// <summary>Version of the IBYS service that was used. (Legacy: <c>IbysVersion</c>)</summary>
    public string? IbysVersion { get; set; }

    /// <summary>Timestamp accompanying the signature. (Legacy: <c>ZamanDamgasi</c>)</summary>
    public string? TimeStamp { get; set; }

    // ---------------- Context ----------------

    /// <summary>Workplace the submission relates to. (Legacy: <c>FirmaId</c>)</summary>
    public int? CompanyId { get; set; }

    /// <summary>Employee the submission relates to (for health report submissions). (Legacy: <c>FirmaPersonelId</c>)</summary>
    public int? CompanyEmployeeId { get; set; }

    // ---------------- Payload ----------------

    /// <summary>
    /// Raw XML sent to IBYS.
    /// <para>
    /// LONG TEXT — configured as <c>nvarchar(max)</c>. Because it contains personal health data
    /// it is treated as an ENCRYPTED COLUMN; the <c>EncryptedStringConverter</c> will be wired up
    /// in phase 2. (Legacy: <c>XmlVeri</c>)
    /// </para>
    /// </summary>
    public string? XmlData { get; set; }

    /// <summary>
    /// E-signed payload (CAdES/base64).
    /// <para>
    /// LONG TEXT — <c>nvarchar(max)</c>. ENCRYPTED COLUMN; the converter will be wired up in
    /// phase 2. (Legacy: <c>ImzaliVeri</c>)
    /// </para>
    /// </summary>
    public string? SignedData { get; set; }
}
