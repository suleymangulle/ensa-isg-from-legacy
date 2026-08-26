using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Companies.Dtos.Navigations;

/// <summary>
/// The combined view the company detail screen needs in a single call.
/// <para>
/// Class-typed properties are not allowed on plain DTOs, so this combination lives in a
/// <see cref="NavigationDto"/> derivative instead (see docs/ARCHITECTURE.md §4).
/// </para>
/// </summary>
public class CompanyNavigationDto : NavigationDto
{
    public CompanyDto Company { get; set; } = null!;

    public LookupDto? City { get; set; }
    public LookupDto? District { get; set; }
    public LookupDto? Neighborhood { get; set; }
    public LookupDto? Office { get; set; }

    /// <summary>For a branch, the headquarter it belongs to.</summary>
    public LookupDto? HeadquarterCompany { get; set; }

    /// <summary>For a headquarter, the branches attached to it.</summary>
    public List<LookupDto> Branches { get; set; } = [];

    public List<AssignedSpecialistDto> AssignedSpecialists { get; set; } = [];
    public List<LookupDto> Departments { get; set; } = [];

    public int ActiveEmployeeCount { get; set; }

    /// <summary>Denormalized warning summary, refreshed by a background job.</summary>
    public CompanyWarningSummaryDto? WarningSummary { get; set; }
}

/// <summary>A specialist or physician assigned to the company.</summary>
public class AssignedSpecialistDto : EntityDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int StaffRole { get; set; }
    public int? MonthlyWorkDurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Counters for the company's outstanding OHS obligations.</summary>
public class CompanyWarningSummaryDto
{
    public int IsSafetyTrainingNoneCount { get; set; }
    public int IsSafetyTrainingMissingCount { get; set; }
    public int IsHealthTrainingNoneCount { get; set; }
    public int IsHealthTrainingMissingCount { get; set; }
    public int PreEmploymentHealthExaminationMissingCount { get; set; }
    public int EquipmentExaminationMissingCount { get; set; }

    public int TotalMissing =>
        IsSafetyTrainingNoneCount + IsSafetyTrainingMissingCount
        + IsHealthTrainingNoneCount + IsHealthTrainingMissingCount
        + PreEmploymentHealthExaminationMissingCount + EquipmentExaminationMissingCount;
}
