using Ensa.Domain.Common;

namespace Ensa.Domain.Finance;

/// <summary>
/// An administrative fine that can be imposed under Law No. 6331, defined per statutory article.
/// <para>Legacy equivalent: <c>Penalty_T</c>.</para>
/// <para>A host reference library with no tenant — shared by every tenant.</para>
/// <para>
/// NORMALIZATION: the nine fixed amount columns in legacy
/// (<c>AzHazardous_K_10_Penalty</c>, <c>AzHazardous_10_ve_49_Penalty</c>, <c>AzHazardous_BE_50</c>,
/// <c>Hazardous_K_10_Penalty</c>, <c>Hazardous_10_ve_49_Penalty</c>, <c>Hazardous_BE_50</c>,
/// <c>VeryHazardous_K_10_Penalty</c>, <c>VeryHazardous_10_ve_49_Penalty</c>, <c>VeryHazardous_BE_50</c>)
/// were REMOVED from the header and normalized into the <see cref="PenaltyAmount"/> child table
/// (<c>HazardClass</c> × <c>EmployeeCountRange</c>). That also makes per-year tracking possible, so
/// amounts can be updated annually with the revaluation rate.
/// </para>
/// </summary>
public class Penalty : FullAuditedEntity, IActivatable
{
    /// <summary>Code of the related node in the legislation tree. (Legacy: <c>TreeItemCode</c>)</summary>
    public string? TreeNodeCode { get; set; }

    public string LawArticle { get; set; } = string.Empty;

    public string PenaltyArticle { get; set; } = string.Empty;

    public string? LawArticleReferencedOffence { get; set; }

    /// <summary>Whether the amount is multiplied by the employee count.</summary>
    public bool MultiplierCalculate { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
