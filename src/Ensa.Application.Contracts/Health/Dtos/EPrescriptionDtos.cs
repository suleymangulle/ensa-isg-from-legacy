using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Health.Dtos;

/// <summary>
/// A single row of the e-prescription list.
/// <para>
/// <b>PRIVACY.</b> Prescribed medication and diagnoses are health data, so the list row
/// carries neither. It shows only the prescription envelope — code, patient reference,
/// dates and the service result — plus counts. Medication and ICD-10 detail is available
/// through <c>GetWithNavigationAsync</c> for one record at a time.
/// </para>
/// </summary>
public class EPrescriptionListDto : EntityDto
{
    public string? EPrescriptionCode { get; set; }

    public string? ProtocolNo { get; set; }

    /// <summary>Patient's national id — required by the e-prescription service.</summary>
    public string PatientNationalId { get; set; } = string.Empty;

    public int? PatientCompanyEmployeeId { get; set; }

    /// <summary>Patient's display name when the prescription is linked to an employee record.</summary>
    public string? PatientFullName { get; set; }

    public bool Cancelled { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public DateTime CreationTime { get; set; }

    public string? ResultCode { get; set; }
}

/// <summary>Full e-prescription header.</summary>
public class EPrescriptionDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string? EPrescriptionCode { get; set; }

    public string? ProtocolNo { get; set; }

    public string PatientNationalId { get; set; } = string.Empty;

    public int? PatientCompanyEmployeeId { get; set; }

    public string? Description { get; set; }

    public PrescriptionNoteType DescriptionType { get; set; }

    public bool Cancelled { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public string? ResultCode { get; set; }

    public string? ResultMessage { get; set; }

    public string? WarningMessage { get; set; }
}

/// <summary>A medication line of an e-prescription.</summary>
public class EPrescriptionMedicationDto : EntityDto
{
    public int EPrescriptionId { get; set; }

    public int MedicationId { get; set; }

    public string? MedicationBarcode { get; set; }

    public int UsageMethodId { get; set; }

    public int UsageDoseUnitId { get; set; }

    public int UsagePeriodUnitId { get; set; }

    public int Box { get; set; }

    public int Dose { get; set; }

    public decimal? DoseFraction { get; set; }

    public int Period { get; set; }

    public string? MedicationDescription { get; set; }

    public PrescriptionNoteType MedicationDescriptionType { get; set; }
}

/// <summary>A diagnosis (ICD-10) line of an e-prescription.</summary>
public class EPrescriptionDiagnosisDto : EntityDto
{
    public int EPrescriptionId { get; set; }

    public string Icd10Code { get; set; } = string.Empty;

    public int? Icd10Id { get; set; }
}

/// <summary>One medication line supplied when creating or updating a prescription.</summary>
public class SaveEPrescriptionMedicationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A medication must be selected.")]
    public int MedicationId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? MedicationBarcode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A route of administration must be selected.")]
    public int UsageMethodId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A dose unit must be selected.")]
    public int UsageDoseUnitId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A frequency unit must be selected.")]
    public int UsagePeriodUnitId { get; set; }

    [Range(1, 100, ErrorMessage = "The box count must be between 1 and 100.")]
    public int Box { get; set; } = 1;

    [Range(0, 1000)]
    public int Dose { get; set; }

    [Range(0, 1000)]
    public decimal? DoseFraction { get; set; }

    [Range(0, 1000)]
    public int Period { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? MedicationDescription { get; set; }

    [EnumDataType(typeof(PrescriptionNoteType))]
    public PrescriptionNoteType MedicationDescriptionType { get; set; } = PrescriptionNoteType.Unspecified;
}

/// <summary>One diagnosis line supplied when creating or updating a prescription.</summary>
public class SaveEPrescriptionDiagnosisDto
{
    [Required(ErrorMessage = "An ICD-10 code is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string Icd10Code { get; set; } = string.Empty;

    /// <summary>SKRS reference when the code exists in the current ICD-10 catalogue.</summary>
    public int? Icd10Id { get; set; }
}

/// <summary>Input used to create an e-prescription together with its lines.</summary>
public class CreateEPrescriptionDto
{
    [Required(ErrorMessage = "The patient's national id is required.")]
    [StringLength(EnsaDomainSharedConsts.MaxLengths.NationalId,
        MinimumLength = EnsaDomainSharedConsts.MaxLengths.NationalId,
        ErrorMessage = "The national id must be 11 digits.")]
    public string PatientNationalId { get; set; } = string.Empty;

    /// <summary>Employee record of the patient; null for external patients.</summary>
    public int? PatientCompanyEmployeeId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? ProtocolNo { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Description { get; set; }

    [EnumDataType(typeof(PrescriptionNoteType))]
    public PrescriptionNoteType DescriptionType { get; set; } = PrescriptionNoteType.Unspecified;

    [MinLength(1, ErrorMessage = "A prescription must contain at least one medication.")]
    public List<SaveEPrescriptionMedicationDto> Medications { get; set; } = [];

    public List<SaveEPrescriptionDiagnosisDto> Diagnoses { get; set; } = [];
}

/// <summary>Input used to update an e-prescription; the line sets are replaced wholesale.</summary>
public class UpdateEPrescriptionDto : CreateEPrescriptionDto;

/// <summary>Filter for the e-prescription list.</summary>
public class GetEPrescriptionListInput : PagedAndSortedFilterDto
{
    public string? PatientNationalId { get; set; }

    public int? PatientCompanyEmployeeId { get; set; }

    public bool? Cancelled { get; set; }

    /// <summary>Lower bound for <c>CreationTime</c>.</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Upper bound for <c>CreationTime</c>.</summary>
    public DateTime? DateTo { get; set; }
}
