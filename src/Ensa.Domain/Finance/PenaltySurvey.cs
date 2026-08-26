using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Finance;

/// <summary>
/// The header of a "penalty exposure" survey filled in for a prospective customer.
/// The answers live in <see cref="PenaltySurveyLine"/>.
/// <para>Legacy equivalent: <c>PenaltySurvey_T</c>.</para>
/// </summary>
public class PenaltySurvey : AuditedTenantEntity
{
    public string CompanyTitle { get; set; } = string.Empty;

    public string? FacilityName { get; set; }

    public string? FacilityOwner { get; set; }

    public string? FacilityOwnerDuty { get; set; }

    /// <summary>(Legacy: <c>TesisSorumlusuGSM</c>)</summary>
    public string? FacilityOwnerGsm { get; set; }

    public string? EmployerNameLastName { get; set; }

    public string? Phone { get; set; }

    /// <summary>(Legacy: <c>Fax</c>)</summary>
    public string? Fax { get; set; }

    /// <summary>(Legacy: <c>EPosta</c>)</summary>
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

    /// <summary>(Legacy: <c>TehlikeSinifi</c> string)</summary>
    public HazardClass HazardClass { get; set; } = HazardClass.Unspecified;

    /// <summary>
    /// The company logo shown on the survey — FK to the central <c>Document</c> table.
    /// (Legacy: <c>byte[] Logo</c>)
    /// </summary>
    public int? LogoDocumentId { get; set; }
}
