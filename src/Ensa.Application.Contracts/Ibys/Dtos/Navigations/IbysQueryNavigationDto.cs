using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Ibys.Dtos.Navigations;

/// <summary>
/// Combined view of an IBYS submission: the submission itself, the workplace and employee
/// it concerns, and the medical examination forms carried by it.
/// <para>
/// <b>SECURITY.</b> Like every other shape in this module, this DTO carries neither
/// <c>XmlData</c> nor <c>SignedData</c>. The attached examination forms are reduced to a
/// clinical-free summary (<see cref="IbysSubmittedFormDto"/>) so that a submission-tracking
/// screen never becomes a back door into health records.
/// </para>
/// </summary>
public class IbysQueryNavigationDto : NavigationDto
{
    public IbysQueryDto Query { get; set; } = null!;

    /// <summary>Workplace the submission concerns, reduced to a lookup.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Employee the submission concerns, reduced to a lookup.</summary>
    public LookupDto? Employee { get; set; }

    /// <summary>Display name of the user who signed the submission.</summary>
    public string? ApproverFullName { get; set; }

    /// <summary>Medical examination forms submitted with this query.</summary>
    public List<IbysSubmittedFormDto> ExaminationForms { get; set; } = [];
}

/// <summary>
/// A medical examination form attached to an IBYS submission, reduced to the fields the
/// submission-tracking screen needs. Carries no clinical content.
/// </summary>
public class IbysSubmittedFormDto : EntityDto
{
    public int CompanyEmployeeId { get; set; }

    public MedicalReportType ReportType { get; set; }

    public DateTime ExaminationDate { get; set; }

    public IbysSubmissionStatus IbysStatus { get; set; }

    public int? IbysStatusCode { get; set; }

    public string? IbysStatusMessage { get; set; }
}
