using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Companies.Dtos.Navigations;

/// <summary>
/// Everything the employee detail screen needs in a single call — identity, employment
/// and the normalized health sub-records.
/// <para>
/// Class-typed properties are forbidden on plain DTOs, so the combination is expressed
/// through a <see cref="NavigationDto"/> derivative (see docs/ARCHITECTURE.md section 4).
/// Mirrors <c>Ensa.Domain.Companies.Navigations.CompanyEmployeeNavigation</c>.
/// </para>
/// </summary>
public class CompanyEmployeeNavigationDto : NavigationDto
{
    public CompanyEmployeeDto CompanyEmployee { get; set; } = null!;

    /// <summary>The workplace the employee belongs to.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>The workplace department the employee is assigned to, when known.</summary>
    public LookupDto? AssignedDepartment { get; set; }

    // ---------------- Health (normalized sub-records) ----------------

    /// <summary>One-to-one health record (blood type, allergies, chronic illnesses).</summary>
    public EmployeeHealthInfoDto? HealthInfo { get; set; }

    public List<EmployeeImmunizationDto> Immunizations { get; set; } = [];

    public List<EmployeeFamilyHistoryDto> FamilyHistory { get; set; } = [];

    public List<EmployeeWorkHistoryDto> WorkHistory { get; set; } = [];

    // ---------------- Duties / training ----------------

    public List<CompanyEmployeeDutyDto> Duties { get; set; } = [];

    /// <summary>Most recent attendance date per training subject (query projection).</summary>
    public List<EmployeeLatestTrainingInfoDto> LatestTrainings { get; set; } = [];
}

/// <summary>Permanent health information of an employee.</summary>
public class EmployeeHealthInfoDto : EntityDto
{
    public int CompanyEmployeeId { get; set; }
    public BloodType BloodType { get; set; }
    public string? AllergyDescription { get; set; }
    public string? ChronicIllnessDescription { get; set; }
}

/// <summary>A single immunization (vaccination) record.</summary>
public class EmployeeImmunizationDto : EntityDto
{
    public int CompanyEmployeeId { get; set; }
    public ImmunizationType ImmunizationType { get; set; }
    public DateTime? Date { get; set; }
    public string? Description { get; set; }
}

/// <summary>A disease reported for a relative of the employee.</summary>
public class EmployeeFamilyHistoryDto : EntityDto
{
    public int CompanyEmployeeId { get; set; }
    public FamilyRelation Relation { get; set; }
    public string? Description { get; set; }
}

/// <summary>A previous employment of the employee.</summary>
public class EmployeeWorkHistoryDto : EntityDto
{
    public int CompanyEmployeeId { get; set; }
    public string? WorkSector { get; set; }
    public string? PerformedJob { get; set; }
    public DateTime? EntryDate { get; set; }
    public DateTime? ExitDate { get; set; }

    /// <summary>Position in the form; 1 is the most recent employment.</summary>
    public int OrderNo { get; set; }
}

/// <summary>An occupational-safety duty assigned to the employee.</summary>
public class CompanyEmployeeDutyDto : EntityDto
{
    public int CompanyEmployeeId { get; set; }
    public int DutyId { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Most recent attendance record of an employee for one training subject.
/// Produced by a <c>GROUP BY ... MAX(DocumentDate)</c> projection; there is no table behind it.
/// </summary>
public class EmployeeLatestTrainingInfoDto
{
    public int CompanyEmployeeId { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public int? TrainingId { get; set; }
    public DateTime? TrainingDate { get; set; }
    public int? CompanyEmployeeDocumentId { get; set; }
}
