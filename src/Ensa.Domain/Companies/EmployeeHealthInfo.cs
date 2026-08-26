using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// An employee's permanent (static) health information. 1-1 with
/// <see cref="CompanyEmployee"/>; <see cref="CompanyEmployeeId"/> is unique.
/// <para>
/// NORMALISATION: the <c>BloodType</c> and <c>Allergy</c> columns and the free-text chronic
/// illness fields of the legacy <c>CompanyEmployee_T</c> were moved here.
/// </para>
/// </summary>
public class EmployeeHealthInfo : FullAuditedTenantEntity
{
    /// <summary>The employee this record belongs to. Unique FK.</summary>
    public int CompanyEmployeeId { get; set; }

    /// <summary>(Legacy: <c>FirmaPersonel_T.KanGrubu</c> string)</summary>
    public BloodType BloodType { get; set; } = BloodType.Unspecified;

    /// <summary>Known allergies. (Legacy: <c>FirmaPersonel_T.Allerji</c>)</summary>
    public string? AllergyDescription { get; set; }

    /// <summary>Known chronic illnesses and any medication taken continuously.</summary>
    public string? ChronicIllnessDescription { get; set; }
}
