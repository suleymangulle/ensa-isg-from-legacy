using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Documents.Dtos;

/// <summary>Document list row.</summary>
public class DocumentListDto : EntityDto
{
    public string DocumentName { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public int? DocumentCategoryId { get; set; }
    public int? CompanyId { get; set; }
    public DocumentOwnerType OwnerType { get; set; }
    public int? OwnerRecordId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// Document metadata.
/// <para>
/// The binary itself is never part of this DTO, and neither are <c>StorageName</c> nor
/// <c>StoragePath</c>: those are internal storage coordinates, and handing them out would
/// invite path traversal and direct-object access against the underlying store.
/// </para>
/// </summary>
public class DocumentDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int? DocumentCategoryId { get; set; }
    public int? CompanyId { get; set; }

    /// <summary>Original file name as the uploader saw it, extension included.</summary>
    public string DocumentName { get; set; } = string.Empty;

    public string? Extension { get; set; }
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>SHA-256 digest of the content, used to detect duplicate uploads.</summary>
    public string? Sha256 { get; set; }

    /// <summary>Module or record type the file is polymorphically attached to.</summary>
    public DocumentOwnerType OwnerType { get; set; }

    /// <summary>Key of the record inside the table <see cref="OwnerType"/> points at.</summary>
    public int? OwnerRecordId { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>Fields shared by document metadata create and update.</summary>
public abstract class DocumentInputDto
{
    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.FileName)]
    public string DocumentName { get; set; } = string.Empty;

    public int? DocumentCategoryId { get; set; }

    public int? CompanyId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? Extension { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.MimeType)]
    public string? ContentType { get; set; }

    [Range(0, long.MaxValue)]
    public long SizeBytes { get; set; }

    /// <summary>SHA-256 digest as 64 lowercase hex characters.</summary>
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Guid)]
    public string? Sha256 { get; set; }

    public DocumentOwnerType OwnerType { get; set; } = DocumentOwnerType.Unspecified;

    public int? OwnerRecordId { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Document metadata create input.</summary>
public class CreateDocumentDto : DocumentInputDto;

/// <summary>Document metadata update input.</summary>
public class UpdateDocumentDto : DocumentInputDto;

/// <summary>Document list filter.</summary>
public class GetDocumentListInput : PagedAndSortedFilterDto
{
    public int? DocumentCategoryId { get; set; }
    public int? CompanyId { get; set; }
    public DocumentOwnerType? OwnerType { get; set; }
    public int? OwnerRecordId { get; set; }
    public bool? IsActive { get; set; }
}
