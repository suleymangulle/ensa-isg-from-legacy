using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks.Dtos;

/// <summary>Field observation report grid row.</summary>
public class FieldObservationReportListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Resolved by the application service with a single batched company lookup.</summary>
    public string? CompanyName { get; set; }

    public int? DepartmentId { get; set; }

    /// <summary>Resolved by the application service with a single batched department lookup.</summary>
    public string? DepartmentName { get; set; }

    public DateTime Date { get; set; }

    /// <summary>Number of non-conformity lines, resolved with one grouped query for the whole page.</summary>
    public int LineCount { get; set; }
}

/// <summary>Field observation report header detail.</summary>
public class FieldObservationReportDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>Field observation report creation input.</summary>
public class CreateFieldObservationReportDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A company must be selected.")]
    public int CompanyId { get; set; }

    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "The observation date is required.")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Legacy <c>[NotMapped] MailGonder</c>: request a notification mail after saving.
    /// Not persisted on the entity; consumed by the application service only.
    /// </summary>
    public bool SendMail { get; set; }

    /// <summary>Legacy <c>[NotMapped] MailAddress</c>. Not persisted on the entity.</summary>
    [EmailAddress(ErrorMessage = "A valid e-mail address is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string? MailAddress { get; set; }
}

/// <summary>Field observation report update input.</summary>
public class UpdateFieldObservationReportDto : CreateFieldObservationReportDto;

/// <summary>Field observation report list filter.</summary>
public class GetFieldObservationReportListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

/// <summary>A single non-conformity line of a field observation report.</summary>
public class FieldObservationLineDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int FieldObservationReportId { get; set; }
    public DateTime? Date { get; set; }
    public DateTime? DeadlineDate { get; set; }

    public string NonConformity { get; set; } = string.Empty;
    public string? Measures { get; set; }

    public string? Owner { get; set; }
    public int? OwnerCompanyEmployeeId { get; set; }

    public RiskCategory RiskCategory { get; set; }
    public int? DocumentId { get; set; }

    /// <summary>True when the line is still open and its deadline is behind the reference date.</summary>
    public bool IsOverdue { get; set; }
}

/// <summary>Field observation line creation input.</summary>
public class CreateFieldObservationLineDto
{
    public DateTime? Date { get; set; }

    public DateTime? DeadlineDate { get; set; }

    [Required(ErrorMessage = "The non-conformity description is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string NonConformity { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Measures { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? Owner { get; set; }

    public int? OwnerCompanyEmployeeId { get; set; }

    public RiskCategory RiskCategory { get; set; } = RiskCategory.Unspecified;

    public int? DocumentId { get; set; }
}

/// <summary>Field observation line update input.</summary>
public class UpdateFieldObservationLineDto : CreateFieldObservationLineDto;
