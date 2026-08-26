using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Health.Navigations;

/// <summary>
/// Combined read model for an e-prescription: the header plus the medication lines (with
/// medication names), the diagnosis lines (with ICD-10 names) and the patient.
/// <para>
/// <c>[NotMapped]</c> — never exposed as a <c>DbSet</c> and never registered with
/// <c>ModelBuilder</c>; it is populated by projection inside
/// <c>IEPrescriptionRepository.GetWithNavigationAsync</c>.
/// </para>
/// </summary>
[NotMapped]
public class EPrescriptionNavigation : NavigationEntity<EPrescription>
{
    /// <summary>Shortcut to the root record (the same instance as <see cref="NavigationEntity{TEntity}.Entity"/>).</summary>
    public EPrescription EPrescription
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>
    /// The employee the prescription was issued for (when
    /// <c>EPrescription.PatientCompanyEmployeeId</c> is set). It is <c>null</c> for external
    /// patients; in that case only <c>EPrescription.PatientNationalId</c> is available.
    /// </summary>
    public CompanyEmployee? Patient { get; set; }

    /// <summary>Medication lines — enriched with the SKRS names.</summary>
    public List<EPrescriptionMedicationNavigation> Medications { get; set; } = [];

    /// <summary>Diagnosis lines — enriched with the ICD-10 names.</summary>
    public List<EPrescriptionDiagnosisNavigation> Diagnoses { get; set; } = [];
}

/// <summary>Prescription medication line plus the SKRS lookup names.</summary>
[NotMapped]
public class EPrescriptionMedicationNavigation : NavigationEntity<EPrescriptionMedication>
{
    public EPrescriptionMedication Medication
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>SKRS medication name (<c>Medication.MedicationName</c>).</summary>
    public string? MedicationName { get; set; }

    /// <summary>SKRS administration route name (<c>MedicationRoute.Name</c>).</summary>
    public string? UsageMethodName { get; set; }

    /// <summary>SKRS dose unit name (<c>MedicationDoseUnit.Name</c>).</summary>
    public string? DoseUnitName { get; set; }

    /// <summary>SKRS frequency unit name (<c>MedicationFrequencyUnit.Name</c>).</summary>
    public string? PeriodUnitName { get; set; }
}

/// <summary>Prescription diagnosis line plus the ICD-10 lookup name.</summary>
[NotMapped]
public class EPrescriptionDiagnosisNavigation : NavigationEntity<EPrescriptionDiagnosis>
{
    public EPrescriptionDiagnosis Diagnosis
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>ICD-10 diagnosis name (<c>Icd10.Name</c>).</summary>
    public string? Icd10Name { get; set; }
}
