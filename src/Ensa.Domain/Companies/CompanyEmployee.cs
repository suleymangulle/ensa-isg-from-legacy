using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// An employee working at a client workplace (identity plus employment details).
/// <para>Legacy equivalent: <c>CompanyEmployee_T</c>.</para>
/// <para>
/// NORMALISATION: the health data that sat in flat columns on this table in the legacy schema
/// has been split out into <see cref="EmployeeHealthInfo"/> (1-1),
/// <see cref="EmployeeImmunization"/> (1-N), <see cref="EmployeeFamilyHistory"/> (1-N) and
/// <see cref="EmployeeWorkHistory"/> (1-N).
/// </para>
/// <para>
/// Distance-learning credentials are no longer held here; they are managed through ASP.NET Core
/// Identity and linked via <see cref="UserId"/>. (The legacy <c>Password</c>/
/// <c>PasswordDegisti</c> columns were removed.)
/// </para>
/// </summary>
public class CompanyEmployee : FullAuditedTenantEntity, IActivatable, ICompanyScoped
{
    /// <summary>The workplace the employee works at.</summary>
    public int CompanyId { get; set; }

    // ---------------- Identity ----------------

    public string Name { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? FatherName { get; set; }

    /// <summary>Mother's name. (Legacy: <c>AnaAdi</c>)</summary>
    public string? MotherName { get; set; }

    /// <summary>11-digit national ID. Unique within the company.</summary>
    public string? NationalId { get; set; }

    public string? BirthLocation { get; set; }

    public DateTime? BirthDate { get; set; }

    /// <summary>(Legacy: string)</summary>
    public Gender Gender { get; set; } = Gender.Unspecified;

    /// <summary>Level of education. (Legacy: <c>EgitimDurumu</c> string)</summary>
    public EducationLevel EducationLevel { get; set; } = EducationLevel.Unspecified;

    /// <summary>(Legacy: <c>MedeniHali</c> string)</summary>
    public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Unspecified;

    public int? ChildCount { get; set; }

    // ---------------- Contact ----------------

    public string? Phone { get; set; }

    /// <summary>Mobile phone. (Legacy: <c>GSM</c>)</summary>
    public string? Gsm { get; set; }

    public string? Email { get; set; }

    public string? HomeAddress { get; set; }

    public string? EmergencyPerson { get; set; }

    public string? EmergencyPersonPhone { get; set; }

    // ---------------- Employment ----------------

    /// <summary>Free-text duty/title.</summary>
    public string? Duty { get; set; }

    /// <summary>Occupation code reference (host table <c>OccupationCode</c>). (Legacy: <c>MeslekKodu</c> string)</summary>
    public int? OccupationCodeId { get; set; }

    /// <summary>Free-text occupation name. (Legacy: <c>Meslegi</c>)</summary>
    public string? Occupation { get; set; }

    /// <summary>
    /// Reference to the workplace department the employee is assigned to.
    /// (Legacy: <c>CalistigiBolum</c> was free text; it was normalised into an FK to
    /// <see cref="WorkplaceDepartment"/>.)
    /// </summary>
    public int? AssignedDepartmentId { get; set; }

    /// <summary>
    /// Free-text department name carried over from the legacy system. It is kept to avoid data
    /// loss; <see cref="AssignedDepartmentId"/> is filled in as records are matched up.
    /// </summary>
    public string? AssignedDepartmentName { get; set; }

    public DateTime? HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    // ---------------- Pre-employment examination ----------------

    /// <summary>Result/notes of the pre-employment examination. (Legacy: <c>IseGirisMuayenesi</c>)</summary>
    public string? PreEmploymentExamination { get; set; }

    public DateTime? PreEmploymentExaminationDate { get; set; }

    /// <summary>Date of the next periodic examination.</summary>
    public DateTime? PreEmploymentNextExaminationDate { get; set; }

    /// <summary>Physician who performed the examination (legacy free text).</summary>
    public string? PreEmploymentExaminationPerformedBy { get; set; }

    /// <summary>
    /// Pre-employment examination report — FK to the central <c>Document</c> table.
    /// (Legacy: <c>IseGirisMuayeneDosya</c> byte[] plus the file name/type columns)
    /// </summary>
    public int? PreEmploymentExaminationDocumentId { get; set; }

    // ---------------- IBYS references ----------------

    /// <summary>IBYS work arrangement code. (Legacy: <c>CalismaSekli</c>)</summary>
    public string? WorkMethodCode { get; set; }

    /// <summary>IBYS work environment code. (Legacy: <c>CalismaOrtami</c>)</summary>
    public string? WorkEnvironmentCode { get; set; }

    /// <summary>IBYS work equipment code. (Legacy: <c>IsEkipmanlari</c>)</summary>
    public string? WorkEquipmentCode { get; set; }

    // ---------------- System ----------------

    /// <summary>
    /// The Identity user that signs in to the distance-learning portal.
    /// It replaces the legacy <c>Password</c>/<c>PasswordDegisti</c> columns.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>Free-text note.</summary>
    public string? Description { get; set; }

    /// <summary>Whether the employee is active (still employed). (Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
