using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks.Dtos;

/// <summary>Work equipment grid row.</summary>
public class EquipmentListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Resolved by the application service with a single batched company lookup.</summary>
    public string? CompanyName { get; set; }

    public string EquipmentName { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }

    public DateTime? ExaminationDate { get; set; }
    public DateTime? NextExaminationDate { get; set; }
    public string? ExaminationPerformedBy { get; set; }
    public int? PeriodId { get; set; }

    /// <summary>True when the periodic inspection is missing or its due date has passed.</summary>
    public bool IsInspectionOverdue { get; set; }

    /// <summary>Days left until <see cref="NextExaminationDate"/>; negative when overdue, null when never inspected.</summary>
    public int? RemainingDays { get; set; }

    public bool IsDeletable { get; set; }
}

/// <summary>Work equipment detail.</summary>
public class EquipmentDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }

    public string? ExaminationReport { get; set; }
    public int? ExaminationReportDocumentId { get; set; }
    public string? ExaminationPerformedBy { get; set; }
    public DateTime? ExaminationDate { get; set; }

    /// <summary>Derived from <see cref="ExaminationDate"/> plus the selected period; not client supplied.</summary>
    public DateTime? NextExaminationDate { get; set; }

    public int? PeriodId { get; set; }
    public bool IsDeletable { get; set; }

    /// <summary>True when the periodic inspection is missing or its due date has passed.</summary>
    public bool IsInspectionOverdue { get; set; }
}

/// <summary>Work equipment creation input.</summary>
public class CreateEquipmentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A company must be selected.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "The equipment name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string EquipmentName { get; set; } = string.Empty;

    public EquipmentType EquipmentType { get; set; } = EquipmentType.Unspecified;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? ExaminationReport { get; set; }

    public int? ExaminationReportDocumentId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? ExaminationPerformedBy { get; set; }

    public DateTime? ExaminationDate { get; set; }

    /// <summary>Inspection period; drives the computed next inspection date.</summary>
    public int? PeriodId { get; set; }
}

/// <summary>Work equipment update input.</summary>
public class UpdateEquipmentDto : CreateEquipmentDto
{
    /// <summary>Records created by integrations are locked against user deletion.</summary>
    public bool IsDeletable { get; set; } = true;
}

/// <summary>Work equipment list filter.</summary>
public class GetEquipmentListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public int? PeriodId { get; set; }

    /// <summary>When true, only equipment with a missing or overdue periodic inspection is returned.</summary>
    public bool OnlyOverdueInspection { get; set; }
}

/// <summary>A document attached to a piece of equipment.</summary>
public class EquipmentDocumentDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int EquipmentId { get; set; }
    public int CompanyId { get; set; }
    public int DocumentId { get; set; }
    public int? EquipmentDocumentTypeId { get; set; }

    public string? Description { get; set; }
    public DateTime? ExaminationDate { get; set; }
    public DateTime? ValidityDate { get; set; }
    public string? ExaminationPerformedBy { get; set; }

    public int? ActivityId { get; set; }
    public int? WorkPlanLineId { get; set; }
}

/// <summary>Equipment document creation input.</summary>
public class CreateEquipmentDocumentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A file must be selected.")]
    public int DocumentId { get; set; }

    public int? EquipmentDocumentTypeId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? Description { get; set; }

    public DateTime? ExaminationDate { get; set; }

    public DateTime? ValidityDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? ExaminationPerformedBy { get; set; }

    public int? ActivityId { get; set; }

    public int? WorkPlanLineId { get; set; }
}
