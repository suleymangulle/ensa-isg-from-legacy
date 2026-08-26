using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Trainings.Dtos;

/// <summary>
/// The mandatory duration of a training for one workplace hazard class.
/// <para>
/// Durations are a normalised child collection, never three flat columns: the legacy
/// <c>AzHazardousDuration</c> / <c>HazardousDuration</c> / <c>VeryHazardousDuration</c>
/// triple became one row per hazard class.
/// </para>
/// </summary>
public class TrainingDurationDto
{
    public HazardClass HazardClass { get; set; }

    public int DurationMinutes { get; set; }
}

/// <summary>One duration row of the replacement set supplied when saving a training.</summary>
public class SaveTrainingDurationDto
{
    [EnumDataType(typeof(HazardClass), ErrorMessage = "An unknown hazard class was supplied.")]
    public HazardClass HazardClass { get; set; }

    [Range(0, 100000, ErrorMessage = "The duration must be between 0 and 100000 minutes.")]
    public int DurationMinutes { get; set; }
}

/// <summary>A single row of the training catalogue list.</summary>
public class TrainingListDto : EntityDto
{
    public string TrainingName { get; set; } = string.Empty;

    public string? TrainingCode { get; set; }

    public int? TrainingGroupId { get; set; }

    public TrainingType TrainingType { get; set; }

    public TrainingSubjectGroup TopicGroup { get; set; }

    public bool MandatoryTraining { get; set; }

    public bool DefaultTraining { get; set; }

    public bool IsActive { get; set; }

    /// <summary><c>null</c> means the training is a host-wide shared template.</summary>
    public int? TenantId { get; set; }
}

/// <summary>Training catalogue entry.</summary>
public class TrainingDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string TrainingName { get; set; } = string.Empty;

    public string? TrainingCode { get; set; }

    public int? TrainingGroupId { get; set; }

    public TrainingType TrainingType { get; set; }

    public TrainingSubjectGroup TopicGroup { get; set; }

    public bool MandatoryTraining { get; set; }

    public bool IsActive { get; set; }

    /// <summary>İSG-KATİP (IBYS) training code.</summary>
    public int? IbysTrainingCode { get; set; }

    public bool IncludedInDefaultPlan { get; set; }

    public bool DefaultTraining { get; set; }

    public int DefaultCount { get; set; }

    public int DefaultStartMonthOffset { get; set; }

    /// <summary>Minimum employee count that makes this training mandatory.</summary>
    public int DefaultElementCondition { get; set; }

    /// <summary>Mandatory duration per hazard class — a list, never three columns.</summary>
    public List<TrainingDurationDto> Durations { get; set; } = [];
}

/// <summary>Input used to create a training catalogue entry together with its durations.</summary>
public class CreateTrainingDto
{
    [Required(ErrorMessage = "The training name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string TrainingName { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? TrainingCode { get; set; }

    public int? TrainingGroupId { get; set; }

    [EnumDataType(typeof(TrainingType))]
    public TrainingType TrainingType { get; set; } = TrainingType.BasicTraining;

    [EnumDataType(typeof(TrainingSubjectGroup))]
    public TrainingSubjectGroup TopicGroup { get; set; } = TrainingSubjectGroup.GeneralSubjects;

    public bool MandatoryTraining { get; set; }

    public int? IbysTrainingCode { get; set; }

    public bool IncludedInDefaultPlan { get; set; }

    public bool DefaultTraining { get; set; }

    [Range(0, 12)]
    public int DefaultCount { get; set; }

    [Range(0, 11)]
    public int DefaultStartMonthOffset { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultElementCondition { get; set; }

    /// <summary>
    /// Mandatory duration per hazard class. At most one row per hazard class; the whole set
    /// is replaced on every save.
    /// </summary>
    public List<SaveTrainingDurationDto> Durations { get; set; } = [];
}

/// <summary>Input used to update a training catalogue entry.</summary>
public class UpdateTrainingDto : CreateTrainingDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Filter for the training catalogue list.</summary>
public class GetTrainingListInput : PagedAndSortedFilterDto
{
    public int? TrainingGroupId { get; set; }

    public TrainingType? TrainingType { get; set; }

    public TrainingSubjectGroup? TopicGroup { get; set; }

    public bool? MandatoryTraining { get; set; }

    public bool? DefaultTraining { get; set; }

    public bool? IsActive { get; set; }
}

// ---------------------------------------------------------------------------
// Topics
// ---------------------------------------------------------------------------

/// <summary>The duration of a single training topic for one hazard class.</summary>
public class TrainingTopicDurationDto
{
    public HazardClass HazardClass { get; set; }

    public int DurationMinutes { get; set; }
}

/// <summary>One topic duration row of the replacement set supplied when saving a topic.</summary>
public class SaveTrainingTopicDurationDto
{
    [EnumDataType(typeof(HazardClass), ErrorMessage = "An unknown hazard class was supplied.")]
    public HazardClass HazardClass { get; set; }

    [Range(0, 100000, ErrorMessage = "The duration must be between 0 and 100000 minutes.")]
    public int DurationMinutes { get; set; }
}

/// <summary>A topic (slide deck section) of a training.</summary>
public class TrainingTopicDto : EntityDto
{
    public int TrainingId { get; set; }

    public string TopicTitle { get; set; } = string.Empty;

    /// <summary>Address of the remote-learning presentation file.</summary>
    public string? PresentationAddress { get; set; }

    public int PresentationPageCount { get; set; }

    /// <summary>Display order within the training.</summary>
    public int TopicOrder { get; set; }

    /// <summary>Topic duration per hazard class — a list, never three columns.</summary>
    public List<TrainingTopicDurationDto> Durations { get; set; } = [];
}

/// <summary>Input used to create a training topic together with its durations.</summary>
public class CreateTrainingTopicDto
{
    [Required(ErrorMessage = "The topic title is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string TopicTitle { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Url)]
    public string? PresentationAddress { get; set; }

    [Range(0, 10000)]
    public int PresentationPageCount { get; set; }

    [Range(0, 10000)]
    public int TopicOrder { get; set; }

    public List<SaveTrainingTopicDurationDto> Durations { get; set; } = [];
}

/// <summary>Input used to update a training topic.</summary>
public class UpdateTrainingTopicDto : CreateTrainingTopicDto;

/// <summary>
/// Result of the statutory refresh calculation owned by <c>ITrainingPlanningManager</c>
/// (low 3 years, hazardous 2 years, very hazardous 1 year; 480 / 720 / 960 minutes).
/// </summary>
public class TrainingValidityDto
{
    public int CompanyEmployeeId { get; set; }

    public int TrainingId { get; set; }

    public HazardClass HazardClass { get; set; }

    /// <summary>Whether the employee's training is still within its refresh interval.</summary>
    public bool IsValid { get; set; }

    /// <summary>Mandatory total training duration for the hazard class, in minutes.</summary>
    public int MandatoryDurationMinutes { get; set; }
}
