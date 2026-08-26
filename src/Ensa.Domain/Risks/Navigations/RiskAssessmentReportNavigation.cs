using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;

namespace Ensa.Domain.Risks.Navigations;

/// <summary>
/// Combined view of a risk assessment report with all of its child records.
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never declared as a <c>DbSet</c>, and never reaches a migration.
/// <c>IRiskAssessmentReportRepository.GetWithNavigationAsync</c> populates it through an
/// <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class RiskAssessmentReportNavigation : NavigationEntity
{
    /// <summary>The root report record.</summary>
    public RiskAssessmentReport Report { get; set; } = null!;

    /// <summary>Summary of the company the report belongs to.</summary>
    public Company? Company { get; set; }

    /// <summary>The OHS specialist user who prepared the report (<c>Report.SpecialistUserId</c>).</summary>
    public User? Specialist { get; set; }

    /// <summary>The workplace physician user who took part in the report (<c>Report.PhysicianUserId</c>).</summary>
    public User? Physician { get; set; }

    /// <summary>The identified hazards; each carries its own control measures.</summary>
    public List<IdentifiedHazardNavigation> IdentifiedHazards { get; set; } = [];

    /// <summary>Groups exposed to the hazard (the replacement for the legacy <c>TMK*</c> boolean columns).</summary>
    public List<RiskAssessmentExposedGroup> ExposedGroups { get; set; } = [];

    /// <summary>Existing control measures (the replacement for the legacy <c>MKO*</c> boolean columns).</summary>
    public List<RiskAssessmentControlMeasure> ProtectionMeasures { get; set; } = [];

    /// <summary>Improvement actions (the replacement for the legacy <c>IO*</c> boolean columns).</summary>
    public List<RiskAssessmentImprovementAction> ImprovementActions { get; set; } = [];

    /// <summary>Worker groups that require a special policy.</summary>
    public List<RiskAssessmentProtectedGroup> SpecialGroups { get; set; } = [];

    /// <summary>Members of the risk assessment team (the replacement for the legacy CSV string columns).</summary>
    public List<RiskAssessmentParticipant> Participants { get; set; } = [];

    /// <summary>Past work accident, no-damage accident, occupational disease and near miss records (four separate tables in legacy).</summary>
    public List<RiskAssessmentHistoryRecord> HistoryRecords { get; set; } = [];
}

/// <summary>
/// An identified hazard line with its library details and the control measures attached to it.
/// </summary>
[NotMapped]
public class IdentifiedHazardNavigation : NavigationEntity
{
    /// <summary>The root hazard line.</summary>
    public IdentifiedHazard IdentifiedHazard { get; set; } = null!;

    /// <summary>The hazard category, when it was picked from the library.</summary>
    public HazardCategory? Category { get; set; }

    /// <summary>The source hazard record, when it was picked from the library.</summary>
    public Hazard? LibraryHazard { get; set; }

    /// <summary>The control measures defined for this hazard.</summary>
    public List<ControlMeasure> ControlMeasures { get; set; } = [];
}
