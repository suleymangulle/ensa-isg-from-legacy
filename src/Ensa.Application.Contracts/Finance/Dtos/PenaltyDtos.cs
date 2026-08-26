using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Finance.Dtos;

/// <summary>Statutory fine list row.</summary>
public class PenaltyListDto : EntityDto
{
    public string? TreeNodeCode { get; set; }
    public string LawArticle { get; set; } = string.Empty;
    public string PenaltyArticle { get; set; } = string.Empty;
    public bool MultiplierCalculate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Statutory fine detail view.
/// <para>
/// This is a HOST catalogue record and therefore carries no <c>TenantId</c>: the fines laid down
/// by law are shared by every organization, so only host administrators may change them.
/// </para>
/// </summary>
public class PenaltyDto : FullAuditedEntityDto
{
    public string? TreeNodeCode { get; set; }
    public string LawArticle { get; set; } = string.Empty;
    public string PenaltyArticle { get; set; } = string.Empty;
    public string? LawArticleReferencedOffence { get; set; }

    /// <summary>Whether the amount is multiplied by the head count of the workplace.</summary>
    public bool MultiplierCalculate { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>One cell of the fine matrix: hazard class x head-count band x year.</summary>
public class PenaltyAmountDto : AuditedEntityDto
{
    public int PenaltyId { get; set; }
    public HazardClass HazardClass { get; set; }
    public EmployeeCountRange EmployeeCountRange { get; set; }
    public decimal Amount { get; set; }
    public int ValidityYear { get; set; }
}

/// <summary>Statutory fine creation input.</summary>
public class CreatePenaltyDto
{
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? TreeNodeCode { get; set; }

    [Required(ErrorMessage = "The law article is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string LawArticle { get; set; } = string.Empty;

    [Required(ErrorMessage = "The penalty article is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string PenaltyArticle { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? LawArticleReferencedOffence { get; set; }

    public bool MultiplierCalculate { get; set; }
}

/// <summary>Statutory fine update input.</summary>
public class UpdatePenaltyDto : CreatePenaltyDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Fine matrix cell creation input.</summary>
public class CreatePenaltyAmountDto
{
    public HazardClass HazardClass { get; set; } = HazardClass.LowHazard;

    public EmployeeCountRange EmployeeCountRange { get; set; } = EmployeeCountRange.FewerThanTen;

    [Range(0, 999999999.99, ErrorMessage = "The amount cannot be negative.")]
    public decimal Amount { get; set; }

    [Range(2000, 2200, ErrorMessage = "The validity year is out of range.")]
    public int ValidityYear { get; set; }
}

/// <summary>Fine matrix cell update input.</summary>
public class UpdatePenaltyAmountDto : CreatePenaltyAmountDto;

/// <summary>Statutory fine list filter.</summary>
public class GetPenaltyListInput : PagedAndSortedFilterDto
{
    public bool? IsActive { get; set; }
    public bool? MultiplierCalculate { get; set; }
}

/// <summary>The amount that applies to one workplace profile for one year.</summary>
public class ApplicablePenaltyAmountDto
{
    public int PenaltyId { get; set; }
    public HazardClass HazardClass { get; set; }
    public EmployeeCountRange EmployeeCountRange { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
}

// ------------------------------------------------------------------ Fine survey

/// <summary>Fine-risk survey list row.</summary>
public class PenaltySurveyListDto : EntityDto
{
    public string CompanyTitle { get; set; } = string.Empty;
    public string? FacilityName { get; set; }
    public HazardClass HazardClass { get; set; }
    public int? WorkerCount { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>Fine-risk survey header, filled in for a prospective customer.</summary>
public class PenaltySurveyDto : AuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string CompanyTitle { get; set; } = string.Empty;
    public string? FacilityName { get; set; }
    public string? FacilityOwner { get; set; }
    public string? FacilityOwnerDuty { get; set; }
    public string? FacilityOwnerGsm { get; set; }
    public string? EmployerNameLastName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public int? CityId { get; set; }
    public int? DistrictId { get; set; }
    public int? NeighborhoodId { get; set; }
    public string? Address { get; set; }
    public string? InvoiceAddress { get; set; }
    public string? TaxTaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public int? WorkerCount { get; set; }
    public string? SsiRegistrationNumber { get; set; }
    public HazardClass HazardClass { get; set; }
    public int? LogoDocumentId { get; set; }
}

/// <summary>One answered fine article inside a survey.</summary>
public class PenaltySurveyLineDto : CreationAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int PenaltySurveyId { get; set; }
    public int PenaltyId { get; set; }

    /// <summary><c>true</c> means the workplace is in breach of this article.</summary>
    public bool SurveyAnswer { get; set; }

    /// <summary>Resolved from the fine catalogue on the server; never taken from the client.</summary>
    public decimal PenaltyAmount { get; set; }

    /// <summary>Head-count multiplier applied when <see cref="MultiplierCalculate"/> is set.</summary>
    public decimal Multiplier { get; set; }

    public bool MultiplierCalculate { get; set; }
}

/// <summary>Fine-risk survey creation input.</summary>
public class CreatePenaltySurveyDto
{
    [Required(ErrorMessage = "The company title is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string CompanyTitle { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string? FacilityName { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? FacilityOwner { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? FacilityOwnerDuty { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? FacilityOwnerGsm { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? EmployerNameLastName { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Phone { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Fax { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string? Email { get; set; }

    public int? CityId { get; set; }
    public int? DistrictId { get; set; }
    public int? NeighborhoodId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string? Address { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string? InvoiceAddress { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? TaxTaxOffice { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.TaxNo)]
    public string? TaxNumber { get; set; }

    [Range(0, 1000000, ErrorMessage = "The head count is out of range.")]
    public int? WorkerCount { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? SsiRegistrationNumber { get; set; }

    public HazardClass HazardClass { get; set; } = HazardClass.Unspecified;

    public int? LogoDocumentId { get; set; }
}

/// <summary>Fine-risk survey update input.</summary>
public class UpdatePenaltySurveyDto : CreatePenaltySurveyDto;

/// <summary>
/// Survey answer input.
/// <para>
/// The amount is intentionally absent: it is resolved on the server from the
/// <see cref="PenaltyAmountDto"/> matrix using the survey's own hazard class and head count, so
/// a client cannot inflate or deflate a fine exposure figure.
/// </para>
/// </summary>
public class CreatePenaltySurveyLineDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A fine article must be selected.")]
    public int PenaltyId { get; set; }

    public bool SurveyAnswer { get; set; }

    /// <summary>Year whose fine schedule applies. Defaults to the current year when omitted.</summary>
    [Range(2000, 2200, ErrorMessage = "The year is out of range.")]
    public int? Year { get; set; }
}

/// <summary>Survey answer update input.</summary>
public class UpdatePenaltySurveyLineDto : CreatePenaltySurveyLineDto;

/// <summary>Fine-risk survey list filter.</summary>
public class GetPenaltySurveyListInput : PagedAndSortedFilterDto
{
    public HazardClass? HazardClass { get; set; }
    public int? CityId { get; set; }
}

/// <summary>Survey line list filter.</summary>
public class GetPenaltySurveyLineListInput : PagedAndSortedRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A survey must be selected.")]
    public int PenaltySurveyId { get; set; }

    /// <summary>When set, only breached (or only compliant) articles are returned.</summary>
    public bool? SurveyAnswer { get; set; }
}

/// <summary>Total fine exposure computed from the answered survey lines.</summary>
public class PenaltySurveyTotalDto
{
    public int PenaltySurveyId { get; set; }

    /// <summary>Number of answered articles in the survey.</summary>
    public int LineCount { get; set; }

    /// <summary>Number of articles the workplace is in breach of.</summary>
    public int ViolationCount { get; set; }

    /// <summary>Sum of the breached articles, head-count multiplier applied where applicable.</summary>
    public decimal TotalAmount { get; set; }
}
