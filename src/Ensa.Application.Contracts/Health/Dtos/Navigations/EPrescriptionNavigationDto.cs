using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Health.Dtos.Navigations;

/// <summary>
/// Combined view of an e-prescription: header, patient, medication lines enriched with
/// SKRS names and diagnosis lines enriched with ICD-10 names.
/// <para>
/// <b>PRIVACY.</b> Medication and diagnosis detail is health data; it is exposed only
/// through this single-record shape, never in a list payload.
/// </para>
/// </summary>
public class EPrescriptionNavigationDto : NavigationDto
{
    public EPrescriptionDto EPrescription { get; set; } = null!;

    /// <summary>Patient's employee record when the prescription is linked to one.</summary>
    public LookupDto? Patient { get; set; }

    public List<EPrescriptionMedicationLineDto> Medications { get; set; } = [];

    public List<EPrescriptionDiagnosisLineDto> Diagnoses { get; set; } = [];
}

/// <summary>A medication line together with its resolved SKRS display names.</summary>
public class EPrescriptionMedicationLineDto : EntityDto
{
    public int MedicationId { get; set; }

    /// <summary>SKRS medication trade name.</summary>
    public string? MedicationName { get; set; }

    public string? MedicationBarcode { get; set; }

    public int UsageMethodId { get; set; }

    /// <summary>SKRS route of administration name.</summary>
    public string? UsageMethodName { get; set; }

    public int UsageDoseUnitId { get; set; }

    /// <summary>SKRS dose unit name.</summary>
    public string? DoseUnitName { get; set; }

    public int UsagePeriodUnitId { get; set; }

    /// <summary>SKRS frequency unit name.</summary>
    public string? PeriodUnitName { get; set; }

    public int Box { get; set; }

    public int Dose { get; set; }

    public decimal? DoseFraction { get; set; }

    public int Period { get; set; }

    public string? MedicationDescription { get; set; }
}

/// <summary>A diagnosis line together with its resolved ICD-10 name.</summary>
public class EPrescriptionDiagnosisLineDto : EntityDto
{
    public string Icd10Code { get; set; } = string.Empty;

    public int? Icd10Id { get; set; }

    /// <summary>ICD-10 diagnosis name.</summary>
    public string? Icd10Name { get; set; }
}
