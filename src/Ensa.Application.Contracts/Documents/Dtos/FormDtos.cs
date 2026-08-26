using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;

namespace Ensa.Application.Contracts.Documents.Dtos;

/// <summary>Form list row.</summary>
public class FormListDto : EntityDto
{
    public string FormName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? DocumentId { get; set; }
    public bool IsActive { get; set; }
    public bool DefaultForm { get; set; }
}

/// <summary>A downloadable form or template definition.</summary>
public class FormDto : AuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string FormName { get; set; } = string.Empty;

    /// <summary>Key into the central document store holding the file. Metadata only here.</summary>
    public int? DocumentId { get; set; }

    public int CategoryId { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Whether this is the default (featured) form of its category.</summary>
    public bool DefaultForm { get; set; }
}

/// <summary>Fields shared by form create and update.</summary>
public abstract class FormInputDto
{
    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string FormName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A form category must be selected.")]
    public int CategoryId { get; set; }

    /// <summary>Existing document to attach. The binary is uploaded separately.</summary>
    public int? DocumentId { get; set; }

    public bool DefaultForm { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Form create input.</summary>
public class CreateFormDto : FormInputDto;

/// <summary>Form update input.</summary>
public class UpdateFormDto : FormInputDto;

/// <summary>Form list filter.</summary>
public class GetFormListInput : PagedAndSortedFilterDto
{
    public int? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? DefaultForm { get; set; }
}
