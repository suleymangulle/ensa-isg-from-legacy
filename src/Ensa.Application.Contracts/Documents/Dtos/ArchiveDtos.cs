using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Documents.Dtos;

/// <summary>Archive list row.</summary>
public class ArchiveListDto : EntityDto
{
    public DocumentOwnerType ModuleType { get; set; }
    public int ModuleId { get; set; }
    public int DocumentId { get; set; }
    public int CompanyId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? Description { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// An archived document produced by a module operation, tagged with the module, the record
/// and the period it belongs to.
/// </summary>
public class ArchiveDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    /// <summary>Module the archived file was produced by.</summary>
    public DocumentOwnerType ModuleType { get; set; }

    /// <summary>Key of the related record inside that module.</summary>
    public int ModuleId { get; set; }

    /// <summary>Key into the central document store holding the file.</summary>
    public int DocumentId { get; set; }

    public int CompanyId { get; set; }

    /// <summary>Optional reference to a line inside the module record.</summary>
    public int? LineId { get; set; }

    /// <summary>Month the archive entry belongs to (1-12).</summary>
    public int? Month { get; set; }

    public int? Year { get; set; }

    public string? Description { get; set; }

    /// <summary>Module-specific note, kept separate from <see cref="Description"/> in the legacy model.</summary>
    public string? ModuleDescription { get; set; }

    /// <summary>Original creation date carried over from the pre-migration system.</summary>
    public DateTime? PreviousAddDate { get; set; }

    public int? PreviousAddedByUserId { get; set; }
}

/// <summary>Fields shared by archive create and update.</summary>
public abstract class ArchiveInputDto
{
    public DocumentOwnerType ModuleType { get; set; } = DocumentOwnerType.Unspecified;

    [Range(1, int.MaxValue)]
    public int ModuleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A document must be selected.")]
    public int DocumentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A company must be selected.")]
    public int CompanyId { get; set; }

    public int? LineId { get; set; }

    [Range(1, 12)]
    public int? Month { get; set; }

    [Range(1900, 2200)]
    public int? Year { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? Description { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? ModuleDescription { get; set; }
}

/// <summary>Archive create input.</summary>
public class CreateArchiveDto : ArchiveInputDto;

/// <summary>Archive update input.</summary>
public class UpdateArchiveDto : ArchiveInputDto;

/// <summary>Archive list filter.</summary>
public class GetArchiveListInput : PagedAndSortedFilterDto
{
    public DocumentOwnerType? ModuleType { get; set; }
    public int? ModuleId { get; set; }
    public int? CompanyId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}
