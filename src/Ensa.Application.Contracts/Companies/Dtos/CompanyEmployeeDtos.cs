using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Companies.Dtos;

/// <summary>Company employee list row — the columns rendered in the grid.</summary>
public class CompanyEmployeeListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Requires a join; filled in by the repository/projection, not by AutoMapper.</summary>
    public string? CompanyName { get; set; }

    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }

    public Gender Gender { get; set; }
    public string? Duty { get; set; }

    public int? AssignedDepartmentId { get; set; }
    public string? AssignedDepartmentName { get; set; }

    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    public string? Phone { get; set; }
    public string? Gsm { get; set; }
    public string? Email { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>Company employee detail view.</summary>
public class CompanyEmployeeDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }

    // ---------------- Identity ----------------

    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FatherName { get; set; }

    /// <summary>Mother's given name.</summary>
    public string? MotherName { get; set; }

    public string? NationalId { get; set; }
    public string? BirthLocation { get; set; }
    public DateTime? BirthDate { get; set; }

    public Gender Gender { get; set; }
    public EducationLevel EducationLevel { get; set; }
    public MaritalStatus MaritalStatus { get; set; }
    public int? ChildCount { get; set; }

    // ---------------- Contact ----------------

    public string? Phone { get; set; }
    public string? Gsm { get; set; }
    public string? Email { get; set; }
    public string? HomeAddress { get; set; }
    public string? EmergencyPerson { get; set; }
    public string? EmergencyPersonPhone { get; set; }

    // ---------------- Employment ----------------

    public string? Duty { get; set; }
    public int? OccupationCodeId { get; set; }
    public string? Occupation { get; set; }
    public int? AssignedDepartmentId { get; set; }
    public string? AssignedDepartmentName { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    // ---------------- Pre-employment examination ----------------

    public string? PreEmploymentExamination { get; set; }
    public DateTime? PreEmploymentExaminationDate { get; set; }
    public DateTime? PreEmploymentNextExaminationDate { get; set; }
    public string? PreEmploymentExaminationPerformedBy { get; set; }
    public int? PreEmploymentExaminationDocumentId { get; set; }

    // ---------------- IBYS reference codes ----------------

    public string? WorkMethodCode { get; set; }
    public string? WorkEnvironmentCode { get; set; }
    public string? WorkEquipmentCode { get; set; }

    // ---------------- System ----------------

    public int? UserId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Company employee creation input.</summary>
public class CreateCompanyEmployeeDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A workplace must be selected.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "The first name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "The last name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? FatherName { get; set; }

    /// <summary>Mother's given name.</summary>
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? MotherName { get; set; }

    /// <summary>
    /// Eleven-digit national identity number. May be empty for foreign nationals;
    /// the checksum and the per-company uniqueness rule are enforced by
    /// <c>CompanyEmployeeManager</c>.
    /// </summary>
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.NationalId)]
    public string? NationalId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? BirthLocation { get; set; }

    public DateTime? BirthDate { get; set; }

    public Gender Gender { get; set; } = Gender.Unspecified;

    public EducationLevel EducationLevel { get; set; } = EducationLevel.Unspecified;

    public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Unspecified;

    [Range(0, 30, ErrorMessage = "The number of children is not valid.")]
    public int? ChildCount { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Phone { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Gsm { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string? Email { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string? HomeAddress { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? EmergencyPerson { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? EmergencyPersonPhone { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? Duty { get; set; }

    public int? OccupationCodeId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? Occupation { get; set; }

    public int? AssignedDepartmentId { get; set; }

    /// <summary>Free-text department name carried over from the legacy data.</summary>
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? AssignedDepartmentName { get; set; }

    public DateTime? HireDate { get; set; }

    /// <summary>
    /// Set only when the employee is created as already terminated; use
    /// <c>TerminateAsync</c> for the regular exit flow.
    /// </summary>
    public DateTime? TerminationDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? PreEmploymentExamination { get; set; }

    public DateTime? PreEmploymentExaminationDate { get; set; }

    public DateTime? PreEmploymentNextExaminationDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? PreEmploymentExaminationPerformedBy { get; set; }

    public int? PreEmploymentExaminationDocumentId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? WorkMethodCode { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? WorkEnvironmentCode { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? WorkEquipmentCode { get; set; }

    /// <summary>Identity user used for the remote-training portal.</summary>
    public int? UserId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? Description { get; set; }
}

/// <summary>Company employee update input.</summary>
public class UpdateCompanyEmployeeDto : CreateCompanyEmployeeDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Company employee list filter.</summary>
public class GetCompanyEmployeeListInput : PagedAndSortedFilterDto
{
    /// <summary>Free-text search runs over first name, last name and national id.</summary>
    public int? CompanyId { get; set; }

    public bool? IsActive { get; set; }

    public Gender? Gender { get; set; }

    /// <summary>Workplace department the employee is assigned to.</summary>
    public int? DepartmentId { get; set; }
}

/// <summary>Termination (exit) input.</summary>
public class TerminateCompanyEmployeeDto
{
    [Required(ErrorMessage = "The exit date is required.")]
    public DateTime ExitDate { get; set; }
}
