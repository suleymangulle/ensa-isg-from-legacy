using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Companies.Dtos.Navigations;

/// <summary>
/// Everything the department detail screen needs in a single call — the department,
/// its workplace, its documents and the employees assigned to it.
/// <para>
/// Mirrors <c>Ensa.Domain.Companies.Navigations.WorkplaceDepartmentNavigation</c>.
/// </para>
/// </summary>
public class WorkplaceDepartmentNavigationDto : NavigationDto
{
    public WorkplaceDepartmentDto WorkplaceDepartment { get; set; } = null!;

    public LookupDto? Company { get; set; }

    public List<DepartmentDocumentDto> Documents { get; set; } = [];

    /// <summary>Employees currently assigned to this department.</summary>
    public List<LookupDto> Employees { get; set; } = [];

    /// <summary>Number of employees attached to the department; blocks deletion when non-zero.</summary>
    public int EmployeeCount { get; set; }
}

/// <summary>A document attached to a workplace department (measurement report, permit, ...).</summary>
public class DepartmentDocumentDto : EntityDto
{
    public int WorkplaceDepartmentId { get; set; }

    /// <summary>Document type code (free text in the legacy data).</summary>
    public string? DocumentCode { get; set; }

    public string? Description { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int? DocumentId { get; set; }

    public DateTime? ExaminationDate { get; set; }

    public DateTime? ValidityDate { get; set; }

    public string? ExaminationPerformedBy { get; set; }

    public int? ActivityId { get; set; }

    public int? WorkPlanLineId { get; set; }
}
