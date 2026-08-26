using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Communication.Dtos;

/// <summary>Visit list row.</summary>
public class VisitListDto : EntityDto
{
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public DateTime VisitDate { get; set; }
    public VisitType OperationType { get; set; }
    public bool Completed { get; set; }
    public string? Description { get; set; }
}

/// <summary>Visit detail view.</summary>
public class VisitDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public DateTime VisitDate { get; set; }

    /// <summary>Calendar start. Falls back to <see cref="VisitDate"/> when not set.</summary>
    public DateTime? Start { get; set; }

    /// <summary>Calendar end. Falls back to <see cref="Start"/> when not set.</summary>
    public DateTime? End { get; set; }

    public VisitType OperationType { get; set; }
    public string? Description { get; set; }

    /// <summary>Calendar colour, hex.</summary>
    public string? Color { get; set; }

    public int? ScheduledWeek { get; set; }
    public int? ScheduledMonth { get; set; }
    public int? RegionCode { get; set; }

    /// <summary>Distance to the next workplace visited the same day, in kilometres.</summary>
    public decimal? OtherCompanyDistanceKm { get; set; }

    /// <summary>Whether the visit actually took place, as opposed to merely being planned.</summary>
    public bool Completed { get; set; }
}

/// <summary>Visit creation input.</summary>
public class CreateVisitDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A workplace must be selected.")]
    public int CompanyId { get; set; }

    /// <summary>The visiting user. Defaults to the caller when omitted.</summary>
    public int? UserId { get; set; }

    [Required(ErrorMessage = "The visit date is required.")]
    public DateTime VisitDate { get; set; }

    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }

    public VisitType OperationType { get; set; } = VisitType.RoutineVisit;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Description { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Color)]
    public string? Color { get; set; }

    [Range(1, 53, ErrorMessage = "The week number must be between 1 and 53.")]
    public int? ScheduledWeek { get; set; }

    [Range(1, 12, ErrorMessage = "The month must be between 1 and 12.")]
    public int? ScheduledMonth { get; set; }

    public int? RegionCode { get; set; }

    [Range(0, 9999999.99, ErrorMessage = "The distance cannot be negative.")]
    public decimal? OtherCompanyDistanceKm { get; set; }
}

/// <summary>Visit update input.</summary>
public class UpdateVisitDto : CreateVisitDto
{
    public bool Completed { get; set; }
}

/// <summary>Visit list filter.</summary>
public class GetVisitListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public int? UserId { get; set; }
    public VisitType? OperationType { get; set; }
    public bool? Completed { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// One entry of the calendar view, shaped for a calendar UI rather than for a data grid.
/// </summary>
public class VisitCalendarDto
{
    public int Id { get; set; }

    /// <summary>Label shown on the calendar: the description when there is one, else the workplace name.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Start instant. Never null — falls back to the visit date.</summary>
    public DateTime Start { get; set; }

    /// <summary>End instant. Never null — falls back to the start.</summary>
    public DateTime End { get; set; }

    /// <summary>Colour, hex. Falls back to the visiting user's colour when the visit has none.</summary>
    public string? Color { get; set; }

    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }

    public int UserId { get; set; }
    public string? UserFullName { get; set; }

    public VisitType OperationType { get; set; }
    public bool Completed { get; set; }
}
