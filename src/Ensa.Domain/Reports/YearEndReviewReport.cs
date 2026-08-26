using Ensa.Domain.Common;

namespace Ensa.Domain.Reports;

/// <summary>
/// The header of a year-end review report — the annual review prepared by the OHS board, or by the
/// OHS specialist and workplace physician. Its activity lines live in
/// <see cref="YearEndReviewLine"/>.
/// <para>Legacy equivalent: <c>YSDReport_T</c>.</para>
/// </summary>
public class YearEndReviewReport : FullAuditedTenantEntity, IActivatable, ICompanyScoped
{
    public string ReportTitle { get; set; } = string.Empty;

    /// <summary>FK — no navigation property.</summary>
    public int CompanyId { get; set; }

    public int? MaleWorker { get; set; }

    public int? FemaleWorker { get; set; }

    public int? ChildWorker { get; set; }

    public int? YoungWorker { get; set; }

    /// <summary>(Legacy: <c>Tarih</c> string → <c>DateTime</c>)</summary>
    public DateTime ReportDate { get; set; }

    /// <summary>The OHS specialist who prepared the report. (Legacy: <c>Uzman</c>, free text) FK — no navigation property.</summary>
    public int? SpecialistUserId { get; set; }

    /// <summary>The legacy free-text name, preserved so it stays in the report even if the user is deleted.</summary>
    public string? SpecialistFullName { get; set; }

    /// <summary>The workplace physician who prepared the report. (Legacy: <c>Hekim</c>, free text) FK — no navigation property.</summary>
    public int? PhysicianUserId { get; set; }

    public string? PhysicianFullName { get; set; }

    /// <summary>Full name of the employer's representative. (Legacy: <c>Vekil</c>)</summary>
    public string? DeputyFullName { get; set; }

    public bool IsActive { get; set; } = true;
}
