using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Health.Dtos;

/// <summary>
/// An ICD-10 search hit. Read-only host reference data seeded from SKRS —
/// it contains no personal data.
/// </summary>
public class Icd10LookupDto : EntityDto
{
    /// <summary>ICD-10 code, for example "J45.9".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Diagnosis name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Code of the parent branch in the ICD-10 hierarchy.</summary>
    public string? ParentCode { get; set; }

    /// <summary>Depth in the ICD-10 hierarchy.</summary>
    public int? Level { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>A medication search hit from the SKRS catalogue.</summary>
public class MedicationLookupDto : EntityDto
{
    public string MedicationName { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    /// <summary>Marketing authorisation holder.</summary>
    public string? GeneratorCompanyName { get; set; }

    public string? AtcCode { get; set; }

    public string? AtcName { get; set; }

    /// <summary>Prescription class (normal, green, red ...).</summary>
    public string? PrescriptionType { get; set; }

    public bool IsActive { get; set; }
}
