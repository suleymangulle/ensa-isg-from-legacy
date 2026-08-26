using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// A member of the risk assessment team.
/// (Legacy: <c>RiskAnalizRaporu_T.IsyeriCalisanTemsilcisi</c>, <c>SupportStaff</c> and
/// <c>InfoOwnerWorkers</c> — all comma-separated CSV string columns.)
/// <para>
/// Legacy declared the same column twice — once as a <c>List&lt;string&gt;</c> and once as a
/// <c>[Column]</c>-mapped CSV string. This row-based shape removes that duplication and lets a
/// participant be linked to a real employee record (<see cref="CompanyEmployeeId"/>).
/// </para>
/// </summary>
public class RiskAssessmentParticipant : CreationAuditedTenantEntity
{
    /// <summary>FK → <see cref="RiskAssessmentReport"/>.</summary>
    public int RiskAssessmentReportId { get; set; }

    /// <summary>The participant's role: worker representative, support staff, knowledgeable worker and so on.</summary>
    public ReportParticipantType ParticipantType { get; set; }

    /// <summary>
    /// The matching company employee. FK → <c>CompanyEmployee.Id</c>.
    /// It stays <c>null</c> for legacy CSV rows that could not be matched during migration.
    /// </summary>
    public int? CompanyEmployeeId { get; set; }

    /// <summary>The participant's full name — the direct counterpart of the legacy CSV value.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>The participant's title or role at the workplace.</summary>
    public string? Title { get; set; }
}
