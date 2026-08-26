using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Ibys.Dtos;

/// <summary>
/// A single row of the IBYS submission list.
/// <para>
/// <b>SECURITY.</b> No DTO in this module exposes <c>IbysQuery.XmlData</c> or
/// <c>IbysQuery.SignedData</c>. Both are encrypted payloads: the XML carries the
/// employee's clinical examination data, and the signed blob is the CAdES envelope
/// produced with the corporate e-signature. Neither is ever needed by a UI — only by the
/// background submission worker, which reads the entity directly through the repository.
/// The e-signature licence key (<c>ESignatureLicense.License</c>) is a secret and is
/// likewise never mapped to any DTO.
/// </para>
/// </summary>
public class IbysQueryListDto : EntityDto
{
    /// <summary>Query number returned by IBYS.</summary>
    public string? QueryNo { get; set; }

    public IbysQueryType QueryType { get; set; }

    public IbysSubmissionStatus Status { get; set; }

    /// <summary>Raw status code returned by the IBYS service.</summary>
    public int StatusCode { get; set; }

    public DateTime SubmissionDate { get; set; }

    public int? CompanyId { get; set; }

    /// <summary>Workplace name (resolved by the application service).</summary>
    public string? CompanyName { get; set; }

    public int? CompanyEmployeeId { get; set; }

    /// <summary>Employee name (resolved by the application service).</summary>
    public string? EmployeeFullName { get; set; }

    /// <summary>Package identifier for batch submissions.</summary>
    public string? GroupId { get; set; }
}

/// <summary>
/// IBYS submission detail.
/// <para>
/// <b>SECURITY.</b> Deliberately omits <c>XmlData</c> and <c>SignedData</c> — see
/// <see cref="IbysQueryListDto"/>. <see cref="HasXmlData"/> and <see cref="HasSignedData"/>
/// expose only whether those payloads are present, which is all an operator needs in
/// order to reason about the submission state.
/// </para>
/// </summary>
public class IbysQueryDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string? QueryNo { get; set; }

    public IbysQueryType QueryType { get; set; }

    public IbysSubmissionStatus Status { get; set; }

    public int StatusCode { get; set; }

    /// <summary>Message returned by the IBYS service.</summary>
    public string? IbysMessage { get; set; }

    public DateTime SubmissionDate { get; set; }

    public string? GroupId { get; set; }

    /// <summary>IBYS service version used for the submission.</summary>
    public string? IbysVersion { get; set; }

    /// <summary>Timestamp accompanying the signature.</summary>
    public string? TimeStamp { get; set; }

    public int? CompanyId { get; set; }

    public int? CompanyEmployeeId { get; set; }

    /// <summary>Whether an XML payload has been prepared. The payload itself is never exposed.</summary>
    public bool HasXmlData { get; set; }

    /// <summary>Whether the payload has been e-signed. The signature is never exposed.</summary>
    public bool HasSignedData { get; set; }
}

/// <summary>Filter for the IBYS submission list.</summary>
public class GetIbysQueryListInput : PagedAndSortedFilterDto
{
    public IbysQueryType? QueryType { get; set; }

    public IbysSubmissionStatus? Status { get; set; }

    public int? CompanyId { get; set; }

    public int? CompanyEmployeeId { get; set; }

    public string? GroupId { get; set; }

    /// <summary>Lower bound for <c>SubmissionDate</c>.</summary>
    public DateTime? SubmissionDateFrom { get; set; }

    /// <summary>Upper bound for <c>SubmissionDate</c>.</summary>
    public DateTime? SubmissionDateTo { get; set; }
}

/// <summary>
/// Input for a status transition. The transition itself is validated by
/// <c>IIbysSubmissionManager.ValidateStatusTransition</c>.
/// </summary>
public class UpdateIbysQueryStatusDto
{
    [EnumDataType(typeof(IbysSubmissionStatus), ErrorMessage = "An unknown submission status was supplied.")]
    public IbysSubmissionStatus Status { get; set; }

    /// <summary>Message returned by the IBYS service, when there is one.</summary>
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Message { get; set; }

    /// <summary>Query number assigned by IBYS once the submission is accepted.</summary>
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? SubmissionNumber { get; set; }
}
