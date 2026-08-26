using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// HEADER + OUTCOME record of a health surveillance examination form (the EK-2 form).
/// <para>Legacy equivalent: <c>PeriodicExaminationForm_T</c> (over 150 flat columns).</para>
///
/// <para>
/// <b>NORMALISATION.</b> The repeated column groups of the legacy table were split into child
/// tables:
/// <list type="bullet">
/// <item>~23 complaint columns → <see cref="MedicalExamComplaint"/></item>
/// <item>12 physical examination columns → <see cref="MedicalExamPhysicalFinding"/></item>
/// <item>8 laboratory/test columns (+ 5 "YapildiMi" flags) → <see cref="MedicalExamLabTest"/></item>
/// <item>the <c>Smoking*</c> / <c>Alcohol*</c> column group → <see cref="MedicalExamHabit"/></item>
/// <item><c>HighCalis</c>/<c>NightCalis</c>/<c>ShiftCalis</c>/<c>WorkCondition</c>/<c>BedenMentally</c>
/// → <see cref="MedicalExamWorkCondition"/></item>
/// <item><c>BagisiklamaTetanus</c>/<c>BagisiklamaHepatitis</c>/<c>BagisiklamaOther</c>
/// → <see cref="MedicalExamImmunization"/></item>
/// </list>
/// </para>
///
/// <para>
/// <b>DENORMALISATION REMOVED.</b> The legacy form <b>copied in</b> the workplace details
/// (<c>WorkplaceTitle</c>, <c>WorkplaceSGKRegistrationNo</c>, <c>WorkplaceAddress</c>,
/// <c>WorkplaceTel</c>, <c>WorkplaceFax</c>, <c>WorkplaceEmail</c>) and the personal details
/// (<c>TCMembershipNo</c>, <c>Name</c>, <c>LastName</c>, <c>Cinsiyeti</c>, <c>BirthLocation</c>,
/// <c>BirthDate</c>, <c>MedeniStatus</c>, <c>TrainingStatus</c>, <c>ChildCount</c>,
/// <c>TelNo</c>, <c>EvAddress</c>, <c>Occupation</c>, <c>PerformedIs</c>,
/// <c>AssignedDepartment</c>, <c>BloodType</c>, <c>Photo</c>). Those copies are gone; the data is
/// now read from the <c>Company</c> / <c>CompanyEmployee</c> records through
/// <see cref="CompanyId"/> and <see cref="CompanyEmployeeId"/>
/// (see <c>Navigations.MedicalExaminationFormNavigation</c>).
/// The legacy <c>PreviousIsIskolu1..3</c> group moved to <c>EmployeeWorkHistory</c> and the
/// <c>FamilyHistory*</c> columns to <c>EmployeeFamilyHistory</c> (Companies module).
/// </para>
///
/// <para>
/// <b>ENCRYPTION.</b> In the legacy schema almost every text column of this table was marked
/// with <c>[EncryptColumn]</c>. In the new model the fields that carry personal health data are
/// individually annotated with an "encrypted column" note below; the
/// <c>EncryptedStringConverter</c> will be attached to them in phase 2.
/// </para>
///
/// <para>
/// <b>TYPE CONVERSIONS.</b> The legacy schema stored dates and numeric values as <c>string</c>
/// (<c>FormDate</c>, <c>ValidityDate</c>, <c>Boy</c>, <c>WeightKg</c>, <c>BMI</c>, <c>TA</c>,
/// <c>Nb</c>, <c>DocumentId</c>). These were converted to the appropriate <c>DateTime</c> /
/// <c>int</c> / <c>decimal</c> types.
/// </para>
/// </summary>
public class MedicalExaminationForm : FullAuditedTenantEntity, ICompanyScoped
{
    // ---------------- Context ----------------

    /// <summary>Workplace where the examination took place. (Legacy: <c>FirmaId</c>)</summary>
    public int? CompanyId { get; set; }

    /// <summary>The employee who was examined. (Legacy: <c>FirmaPersonelId</c>)</summary>
    public int CompanyEmployeeId { get; set; }

    /// <summary>Report type. (Legacy: <c>RaporTuru</c>, an encrypted string)</summary>
    public MedicalReportType ReportType { get; set; } = MedicalReportType.Unspecified;

    /// <summary>Examination (form) date. (Legacy: <c>FormTarihi</c>, an encrypted string)</summary>
    public DateTime ExaminationDate { get; set; }

    /// <summary>
    /// Date the report expires. (Legacy: <c>GecerlilikTarihi</c>, an encrypted string)
    /// When empty it is derived by <c>IHealthSurveillanceManager.CalculateNextExaminationDate</c>.
    /// </summary>
    public DateTime? ValidityDate { get; set; }

    /// <summary>Workplace physician who performed the examination (<c>User</c> FK). (Legacy: <c>EkleyenKullanici</c>)</summary>
    public int? PhysicianUserId { get; set; }

    // ---------------- Anthropometry / vital signs ----------------

    /// <summary>Height (cm). ENCRYPTED COLUMN. (Legacy: <c>Boy</c> string)</summary>
    public int? HeightCm { get; set; }

    /// <summary>Weight (kg). ENCRYPTED COLUMN. (Legacy: <c>Kilo</c> string)</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>
    /// Body mass index (kg/m²). ENCRYPTED COLUMN. (Legacy: <c>BMI</c> string)
    /// It is derived by <c>IHealthSurveillanceManager.CalculateBmi</c> and also persisted, so
    /// that historical records stay consistent.
    /// </summary>
    public decimal? BodyMassIndex { get; set; }

    /// <summary>
    /// Systolic blood pressure (mmHg). ENCRYPTED COLUMN.
    /// (Legacy: the single <c>TA</c> string column — the "120/80" format is split in two during
    /// migration.)
    /// </summary>
    public int? BloodPressureSystolic { get; set; }

    /// <summary>Diastolic blood pressure (mmHg). ENCRYPTED COLUMN. (Legacy: <c>TA</c>)</summary>
    public int? BloodPressureDiastolic { get; set; }

    /// <summary>Pulse rate (beats per minute). ENCRYPTED COLUMN. (Legacy: <c>Nb</c> string)</summary>
    public int? PulseRate { get; set; }

    // ---------------- Anamnesis ----------------

    /// <summary>
    /// Chronic illness declared at the examination (free text). ENCRYPTED COLUMN.
    /// (Legacy: <c>KronikHastalik</c>)
    /// <para>
    /// The employee's permanent health record is <c>EmployeeHealthInfo</c> in the Companies
    /// module; this field is what was DECLARED at that examination — the two are separate
    /// records.
    /// </para>
    /// </summary>
    public string? ChronicIllnessDeclaration { get; set; }

    // ---------------- Outcome / opinion ----------------

    /// <summary>Fitness-for-work opinion. (Legacy: <c>KanaatVeSonuc1</c>, encrypted free text)</summary>
    public FitnessForWorkOpinion Opinion { get; set; } = FitnessForWorkOpinion.Unspecified;

    /// <summary>
    /// Free-text note on the opinion (conditional working restrictions and the like).
    /// ENCRYPTED COLUMN.
    /// (Legacy: the part of the <c>KanaatVeSonuc1</c> text that could not be reduced to an enum)
    /// </summary>
    public string? OpinionDescription { get; set; }

    /// <summary>The physician's recommendations. ENCRYPTED COLUMN. (Legacy: <c>KanaatVeSonuc2</c>)</summary>
    public string? Recommendations { get; set; }

    /// <summary>
    /// PDF/image output of the form — FK to the central <c>Document</c> table.
    /// (Legacy: <c>DosyaId</c> encrypted string + <c>Photo</c> byte[])
    /// </summary>
    public int? DocumentId { get; set; }

    // ---------------- IBYS submission fields ----------------

    /// <summary>IBYS submission status. (Legacy: <c>IBYSDurum</c> int?)</summary>
    public IbysSubmissionStatus IbysStatus { get; set; } = IbysSubmissionStatus.NotSent;

    /// <summary>
    /// The IBYS query this form was sent with (<c>Ibys.IbysQuery</c> FK).
    /// (Legacy: <c>IBYSSorguId</c>)
    /// <para>
    /// The flat legacy <c>IBYSQueryNo</c> string column was REMOVED; the query number is read
    /// through <c>IbysQuery.QueryNo</c> instead (normalisation).
    /// </para>
    /// </summary>
    public int? IbysQueryId { get; set; }

    /// <summary>Status code returned by IBYS. (Legacy: <c>IBYSDurumKodu</c>)</summary>
    public int? IbysStatusCode { get; set; }

    /// <summary>Status message returned by IBYS. (Legacy: <c>IBYSDurumMesaj</c>)</summary>
    public string? IbysStatusMessage { get; set; }

    /// <summary>Group code the form belongs to in a bulk submission. (Legacy: <c>IBYSGrupKodu</c>)</summary>
    public string? IbysGroupCode { get; set; }

    // ---------------- IBYS content fields ----------------
    // The following are declarations the IBYS XML requires that are specific to the form
    // (independent of the employee record), so they stay on the form. The values correspond to
    // the CODE columns of the Ibys.IbysIsco08OccupationCode / IbysWorkEnvironment /
    // IbysWorkArrangement / IbysWorkEquipment reference tables.

    /// <summary>The employee's ISCO-08 occupation code. (Legacy: <c>IBYSCalisanMeslegi</c>)</summary>
    public string? IbysOccupationCode { get; set; }

    /// <summary>Work environment code(s) — comma separated, as in the legacy system. (Legacy: <c>CalismaOrtami</c>)</summary>
    public string? IbysWorkEnvironmentCodes { get; set; }

    /// <summary>Work arrangement code(s) — comma separated, as in the legacy system. (Legacy: <c>CalismaSekli</c>)</summary>
    public string? IbysWorkArrangementCodes { get; set; }

    /// <summary>Work equipment code(s) in use — comma separated, as in the legacy system. (Legacy: <c>IsEkipmanlari</c>)</summary>
    public string? IbysWorkEquipmentCodes { get; set; }

    // ---------------- Origin ----------------

    /// <summary>
    /// Channel the record was created through (web, mobile, bulk import, ...).
    /// (Legacy: <c>Kaynak</c> string)
    /// </summary>
    public string? Source { get; set; }
}
