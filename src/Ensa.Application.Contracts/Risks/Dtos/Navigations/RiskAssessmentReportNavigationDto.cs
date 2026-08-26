using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Risks.Dtos.Navigations;

/// <summary>
/// Everything the risk assessment report detail screen needs in a single round trip.
/// <para>
/// Mirrors <c>Ensa.Domain.Risks.Navigations.RiskAssessmentReportNavigation</c>. Class-typed
/// properties are forbidden on plain DTOs, so the combined read lives in a
/// <see cref="NavigationDto"/> derivative (see docs/ARCHITECTURE.md).
/// </para>
/// </summary>
public class RiskAssessmentReportNavigationDto : NavigationDto
{
    /// <summary>Root report record.</summary>
    public RiskAssessmentReportDto Report { get; set; } = null!;

    /// <summary>Company the report belongs to.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Occupational safety specialist who prepared the report.</summary>
    public LookupDto? Specialist { get; set; }

    /// <summary>Workplace physician who contributed to the report.</summary>
    public LookupDto? Physician { get; set; }

    /// <summary>Identified hazards, each carrying its own control measures.</summary>
    public List<IdentifiedHazardNavigationDto> IdentifiedHazards { get; set; } = [];

    /// <summary>Exposed person groups flagged on the header.</summary>
    public List<RiskAssessmentExposedGroupDto> ExposedGroups { get; set; } = [];

    /// <summary>Existing control measures flagged on the header.</summary>
    public List<RiskAssessmentControlMeasureDto> ControlMeasures { get; set; } = [];

    /// <summary>Improvement recommendations flagged on the header.</summary>
    public List<RiskAssessmentImprovementActionDto> ImprovementActions { get; set; } = [];

    /// <summary>Vulnerable worker groups present at the workplace.</summary>
    public List<RiskAssessmentProtectedGroupDto> ProtectedGroups { get; set; } = [];

    /// <summary>Risk assessment team members.</summary>
    public List<RiskAssessmentParticipantDto> Participants { get; set; } = [];

    /// <summary>Past accident / occupational disease / near-miss records.</summary>
    public List<RiskAssessmentHistoryRecordDto> HistoryRecords { get; set; } = [];

    /// <summary>Number of hazard lines whose residual risk is still at or above the high level.</summary>
    public int OpenHighRiskHazardCount { get; set; }
}

/// <summary>Identified hazard line together with its library origin and control measures.</summary>
public class IdentifiedHazardNavigationDto : NavigationDto
{
    /// <summary>Root hazard line.</summary>
    public IdentifiedHazardDto IdentifiedHazard { get; set; } = null!;

    /// <summary>Hazard category, when the line was picked from the library.</summary>
    public LookupDto? Category { get; set; }

    /// <summary>Source hazard library record, when the line was picked from the library.</summary>
    public LookupDto? LibraryHazard { get; set; }

    /// <summary>Control measures defined for this hazard.</summary>
    public List<ControlMeasureDto> ControlMeasures { get; set; } = [];
}
