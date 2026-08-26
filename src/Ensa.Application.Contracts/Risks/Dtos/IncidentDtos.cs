using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks.Dtos;

/// <summary>Incident grid row.</summary>
public class IncidentListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Resolved by the application service with a single batched company lookup.</summary>
    public string? CompanyName { get; set; }

    public int DepartmentId { get; set; }

    /// <summary>Resolved by the application service with a single batched department lookup.</summary>
    public string? DepartmentName { get; set; }

    public IncidentType IncidentType { get; set; }
    public AccidentType AccidentType { get; set; }
    public DateTime IncidentDate { get; set; }

    public int? LostWorkDays { get; set; }
    public DateTime? SsiNotificationDate { get; set; }

    /// <summary>Computed by <c>IIncidentManager.CalculateLatestNotificationDate</c>.</summary>
    public DateTime? LatestSsiNotificationDate { get; set; }

    /// <summary>Computed by <c>IIncidentManager.NotificationDurationPassedMi</c>.</summary>
    public bool SsiNotificationOverdue { get; set; }
}

/// <summary>Incident detail.</summary>
public class IncidentDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }
    public int DepartmentId { get; set; }

    public IncidentType IncidentType { get; set; }
    public AccidentType AccidentType { get; set; }
    public DateTime IncidentDate { get; set; }

    public string? Description { get; set; }
    public string? Expression { get; set; }

    public int? DocumentId { get; set; }
    public int? UnitSupervisorId { get; set; }
    public string? SupervisorFullName { get; set; }

    public int? LostWorkDays { get; set; }
    public DateTime? IsPerDate { get; set; }
    public DateTime? SsiNotificationDate { get; set; }

    /// <summary>Computed by <c>IIncidentManager.CalculateLatestNotificationDate</c> (3 working days, act 5510 art. 13).</summary>
    public DateTime? LatestSsiNotificationDate { get; set; }

    /// <summary>Computed by <c>IIncidentManager.NotificationDurationPassedMi</c>.</summary>
    public bool SsiNotificationOverdue { get; set; }

    /// <summary>Computed by <c>IIncidentManager.RemainingNotificationIsDay</c>; negative once overdue.</summary>
    public int? RemainingSsiNotificationWorkDays { get; set; }
}

/// <summary>Incident creation input.</summary>
public class CreateIncidentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A company must be selected.")]
    public int CompanyId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A workplace department must be selected.")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "The incident type is required.")]
    public IncidentType IncidentType { get; set; }

    /// <summary>Only meaningful for accident-like incidents; validated by <c>IIncidentManager</c>.</summary>
    public AccidentType AccidentType { get; set; } = AccidentType.Unspecified;

    [Required(ErrorMessage = "The incident date is required.")]
    public DateTime IncidentDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? Description { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? Expression { get; set; }

    public int? DocumentId { get; set; }

    public int? UnitSupervisorId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? SupervisorFullName { get; set; }

    [Range(0, 100000, ErrorMessage = "The lost work day count cannot be negative.")]
    public int? LostWorkDays { get; set; }

    public DateTime? IsPerDate { get; set; }

    public DateTime? SsiNotificationDate { get; set; }
}

/// <summary>Incident update input.</summary>
public class UpdateIncidentDto : CreateIncidentDto;

/// <summary>Incident list filter.</summary>
public class GetIncidentListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public IncidentType? IncidentType { get; set; }
    public AccidentType? AccidentType { get; set; }

    public DateTime? IncidentFrom { get; set; }
    public DateTime? IncidentTo { get; set; }

    /// <summary>When true, only accidents / occupational diseases not yet notified to the SSI are returned.</summary>
    public bool OnlySsiNotificationPending { get; set; }
}

/// <summary>A person involved in an incident (affected / witness / responder).</summary>
public class IncidentPersonDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int IncidentId { get; set; }
    public IncidentPersonRole PersonType { get; set; }
    public int? CompanyEmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

/// <summary>Incident person creation input.</summary>
public class CreateIncidentPersonDto
{
    [Required(ErrorMessage = "The role of the person is required.")]
    public IncidentPersonRole PersonType { get; set; }

    public int? CompanyEmployeeId { get; set; }

    [Required(ErrorMessage = "The first name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "The last name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string LastName { get; set; } = string.Empty;
}

/// <summary>Aggregated lost work days for an accident frequency / severity rate calculation.</summary>
public class LostWorkDaysSummaryDto
{
    public int CompanyId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalLostWorkDays { get; set; }
}
