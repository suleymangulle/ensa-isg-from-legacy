using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks.Dtos;

/// <summary>Emergency action plan grid row.</summary>
public class EmergencyActionPlanListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Resolved by the application service with a single batched company lookup.</summary>
    public string? ResolvedCompanyName { get; set; }

    /// <summary>Workplace title snapshot stored on the plan itself.</summary>
    public string? CompanyName { get; set; }

    public HazardClass HazardClass { get; set; }
    public DateTime PreparedDate { get; set; }
    public DateTime ValidityDate { get; set; }

    public string? TeamsChief { get; set; }

    /// <summary>True when <see cref="ValidityDate"/> is already behind the reference date.</summary>
    public bool IsExpired { get; set; }

    /// <summary>Days left until <see cref="ValidityDate"/>; negative when already expired.</summary>
    public int RemainingDays { get; set; }
}

/// <summary>Emergency action plan header detail.</summary>
public class EmergencyActionPlanDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }
    public DateTime PreparedDate { get; set; }

    /// <summary>Computed from the hazard class (2 / 4 / 6 years); never supplied by the client.</summary>
    public DateTime ValidityDate { get; set; }

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? RegistrationNo { get; set; }
    public HazardClass HazardClass { get; set; }
    public string? Phone { get; set; }

    public string? TeamsChief { get; set; }
    public string? EmergencyTeam { get; set; }
    public string? WorkerRepresentative { get; set; }
    public string? SupportStaff { get; set; }
    public string? EmployerOrDeputy { get; set; }
    public string? OccupationalSafetySpecialist { get; set; }
    public string? WorkplacePhysician { get; set; }
    public string? ProtectionEmployee { get; set; }

    public int? EvacuationPlanDocumentId { get; set; }
    public int? DocumentId { get; set; }

    /// <summary>Whether the plan was still valid at the moment it was read.</summary>
    public bool IsValid { get; set; }
}

/// <summary>Emergency action plan creation input.</summary>
public class CreateEmergencyActionPlanDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A company must be selected.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "The preparation date is required.")]
    public DateTime PreparedDate { get; set; }

    /// <summary>Drives the validity period (2 / 4 / 6 years). <c>Unspecified</c> is rejected.</summary>
    public HazardClass HazardClass { get; set; } = HazardClass.Unspecified;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string? CompanyName { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string? Address { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? RegistrationNo { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Phone { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? TeamsChief { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? EmergencyTeam { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? WorkerRepresentative { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? SupportStaff { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? EmployerOrDeputy { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? OccupationalSafetySpecialist { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? WorkplacePhysician { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? ProtectionEmployee { get; set; }

    public int? EvacuationPlanDocumentId { get; set; }

    public int? DocumentId { get; set; }
}

/// <summary>Emergency action plan update input.</summary>
public class UpdateEmergencyActionPlanDto : CreateEmergencyActionPlanDto;

/// <summary>Emergency action plan list filter.</summary>
public class GetEmergencyActionPlanListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public HazardClass? HazardClass { get; set; }

    public DateTime? PreparedFrom { get; set; }
    public DateTime? PreparedTo { get; set; }

    /// <summary>When true, only plans whose validity date has already passed are returned.</summary>
    public bool OnlyExpired { get; set; }
}

/// <summary>A single free-text section of an emergency action plan.</summary>
public class EmergencyPlanSectionDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int EmergencyActionPlanId { get; set; }
    public EmergencyPlanSectionType SectionType { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderNo { get; set; }
}

/// <summary>Section upsert input — one row per (plan, section type).</summary>
public class SaveEmergencyPlanSectionDto
{
    [Required(ErrorMessage = "The section type is required.")]
    public EmergencyPlanSectionType SectionType { get; set; }

    /// <summary>Rich text / HTML body of the section.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "The section content is required.")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>A member assigned to an emergency team of the plan.</summary>
public class EmergencyTeamMemberDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int EmergencyActionPlanId { get; set; }
    public int CompanyEmployeeId { get; set; }
    public StaffRole StaffRole { get; set; }
    public EmergencyTeamType TeamType { get; set; }
}

/// <summary>Emergency team member creation input.</summary>
public class CreateEmergencyTeamMemberDto
{
    [Range(1, int.MaxValue, ErrorMessage = "An employee must be selected.")]
    public int CompanyEmployeeId { get; set; }

    public StaffRole StaffRole { get; set; } = StaffRole.Unspecified;

    public EmergencyTeamType TeamType { get; set; } = EmergencyTeamType.Unspecified;
}
