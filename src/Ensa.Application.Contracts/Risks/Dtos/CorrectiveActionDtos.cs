using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks.Dtos;

/// <summary>Corrective / preventive action (DOF) grid row.</summary>
public class CorrectiveActionListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Resolved by the application service with a single batched company lookup.</summary>
    public string? CompanyName { get; set; }

    public string Finding { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public RiskCategory RiskCategory { get; set; }
    public CorrectiveActionStatus OperationResult { get; set; }

    public DateTime? FindingDate { get; set; }
    public DateTime? DeadlineDate { get; set; }
    public DateTime? ResultDate { get; set; }

    /// <summary>True when the action is still open and its deadline is behind the reference date.</summary>
    public bool IsOverdue { get; set; }
}

/// <summary>Corrective / preventive action detail.</summary>
public class CorrectiveActionDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }
    public string Finding { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public string? Result { get; set; }
    public string? Source { get; set; }

    public int? FindingDocumentId { get; set; }
    public int? ResultDocumentId { get; set; }

    public RiskCategory RiskCategory { get; set; }
    public CorrectiveActionStatus OperationResult { get; set; }

    public string? Owner { get; set; }
    public int? OwnerCompanyEmployeeId { get; set; }

    public DateTime? FindingDate { get; set; }
    public DateTime? DeadlineDate { get; set; }
    public DateTime? ResultDate { get; set; }

    public int? FieldObservationLineId { get; set; }

    /// <summary>True when the action is still open and its deadline is behind the reference date.</summary>
    public bool IsOverdue { get; set; }
}

/// <summary>Corrective action creation input.</summary>
public class CreateCorrectiveActionDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A company must be selected.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "The finding is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string Finding { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Recommendation { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Source { get; set; }

    public int? FindingDocumentId { get; set; }

    public RiskCategory RiskCategory { get; set; } = RiskCategory.Unspecified;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? Owner { get; set; }

    public int? OwnerCompanyEmployeeId { get; set; }

    public DateTime? FindingDate { get; set; }

    public DateTime? DeadlineDate { get; set; }

    /// <summary>Set when the action is derived from a field observation line.</summary>
    public int? FieldObservationLineId { get; set; }
}

/// <summary>
/// Corrective action update input. The closing fields (<c>Result</c>, <c>ResultDate</c>,
/// <c>OperationResult</c>) are deliberately absent — closing goes through <c>CloseAsync</c>.
/// </summary>
public class UpdateCorrectiveActionDto : CreateCorrectiveActionDto
{
    public int? ResultDocumentId { get; set; }
}

/// <summary>Corrective action closing input.</summary>
public class CloseCorrectiveActionDto
{
    [Required(ErrorMessage = "The result description is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string Result { get; set; } = string.Empty;

    [Required(ErrorMessage = "The result date is required.")]
    public DateTime ResultDate { get; set; }

    public int? ResultDocumentId { get; set; }
}

/// <summary>Corrective action list filter.</summary>
public class GetCorrectiveActionListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public CorrectiveActionStatus? OperationResult { get; set; }
    public RiskCategory? RiskCategory { get; set; }
    public int? OwnerCompanyEmployeeId { get; set; }
    public int? FieldObservationLineId { get; set; }

    public DateTime? FindingFrom { get; set; }
    public DateTime? FindingTo { get; set; }

    /// <summary>When true, only open actions whose deadline has already passed are returned.</summary>
    public bool OnlyOverdue { get; set; }
}
