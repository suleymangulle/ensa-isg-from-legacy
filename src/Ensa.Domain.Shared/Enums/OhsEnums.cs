namespace Ensa.Domain.Shared.Enums;

// ------------------------------------------------------------------
// Work plan / training plan
// ------------------------------------------------------------------

/// <summary>Completion status of a plan line. (Legacy: Durum int, -1/0/1)</summary>
public enum PlanLineStatus
{
    Planned = 0,
    Completed = 1,
    NotDone = 2,
    Postponed = 3,
    Cancelled = 4
}

/// <summary>Activity type. (Legacy: Aktivite_T.Tur string)</summary>
public enum ActivityType
{
    Activity = 1,
    Document = 2,
    Revision = 3,
    MandatoryDocument = 4
}

/// <summary>Where the training is delivered. (Legacy: EgitimPlaniSatirlari_T.EgitimYeri int)</summary>
public enum TrainingLocation
{
    OnSite = 1,
    OffSite = 2,
    RemoteTraining = 3
}

/// <summary>Training type. (Legacy: EgitimPlaniSatirlari_T.EgitimTuru int)</summary>
public enum TrainingType
{
    BasicTraining = 1,
    RefresherTraining = 2,
    AdditionalTraining = 3
}

/// <summary>Training subject group. (Legacy: the Egitim_T.GenelKonular/SaglikKonulari/TeknikKonular bool triple)</summary>
public enum TrainingSubjectGroup
{
    GeneralSubjects = 1,
    HealthSubjects = 2,
    TechnicalSubjects = 3
}

/// <summary>An action the employee performs during remote training. (Legacy: PersonelIslemEnum)</summary>
public enum EmployeeTrainingAction
{
    SignIn = 1,
    FirstTestAttempt = 2,
    FinalTestAttempt = 3,
    TopicProcessing = 4,
    SignOut = 5,
    FirstTestView = 6,
    FinalTestView = 7,
    TopicCompletion = 8,
    TrainingCompletion = 9,
    PasswordChange = 10
}

/// <summary>Exam attempt record type. (Legacy: TestKayitTipi)</summary>
public enum ExamAttemptType
{
    FirstTest = 1,
    FinalTest = 2
}

/// <summary>
/// Per-company progression (advance) mode for remote training.
/// (Legacy: FirmaEgitimGecis_T.ManuelGecis string "konu"/"sayfa")
/// </summary>
public enum TrainingProgressMode
{
    Topic = 1,
    Page = 2
}

// ------------------------------------------------------------------
// Monthly company check (checklist)
// ------------------------------------------------------------------

/// <summary>
/// Status of a monthly check (checklist) record belonging to a company.
/// (Legacy: FirmaKontrol_T.Durum / FirmaKontrolSatir_T.Durum string "Aktif" etc.)
/// </summary>
public enum CompanyCheckStatus
{
    Unspecified = 0,
    Active = 1,
    Completed = 2,
    Approved = 3,
    Cancelled = 4
}

// ------------------------------------------------------------------
// Employee documents / teams
// ------------------------------------------------------------------

/// <summary>
/// Indicates which workplace team an employee document belongs to.
/// (Legacy: the FirmaPersonelDosya_T.RiskDegerlendirmeEkibiDosyasi /
/// AcilDurumEkibiDosyasi / IsgKuruluDosyasi bool triple)
/// </summary>
public enum EmployeeTeamDocumentType
{
    None = 0,
    RiskAssessmentTeam = 1,
    EmergencyTeam = 2,
    OhsCommittee = 3
}

// ------------------------------------------------------------------
// Risk assessment / field
// ------------------------------------------------------------------

/// <summary>Risk assessment methodology. (Legacy: RiskAnalizRaporu_T.RaporMetodu string)</summary>
public enum RiskAssessmentMethod
{
    Unspecified = 0,
    LMatrixThreeByThree = 1,
    LMatrixFiveByFive = 2,
    FineKinney = 3,
    Fmea = 4,
    Checklist = 5
}

/// <summary>The nature of the risk that was identified. (Legacy: DOF_T.Risk / SahaGozlem Risk string)</summary>
public enum RiskCategory
{
    Unspecified = 0,
    WorkAccidentRisk = 1,
    OccupationalDiseaseRisk = 2,
    EnvironmentalRisk = 3,
    FireRisk = 4
}

/// <summary>Outcome of a corrective action. (Legacy: DOF_T.IslemSonucu int; 0/1/-1)</summary>
public enum CorrectiveActionStatus
{
    InProgress = 0,
    Closed = 1,
    Cancelled = 2
}

/// <summary>The group exposed to the hazard in a risk assessment. (Legacy: the RiskAnalizRaporu_T.TMK* bool columns)</summary>
public enum ExposedPersonGroup
{
    ProductionEmployee = 1,
    MaintenanceEmployee = 2,
    Contractors = 3,
    TechnicalEmployee = 4,
    OfficeStaff = 5,
    AuditEmployee = 6,
    Visitors = 7,
    CleaningEmployee = 8,
    EmergencyEmployee = 9,
    Others = 10
}

/// <summary>An existing protective measure. (Legacy: the RiskAnalizRaporu_T.MKO* bool columns)</summary>
public enum ExistingControlMeasure
{
    LocalVentilation = 1,
    MachineGuards = 2,
    PersonalProtectiveUsage = 3,
    FireProtection = 4,
    EmergencyProcedures = 5,
    TrainingAndAwareness = 6,
    WarningSigns = 7
}

/// <summary>A suggested improvement. (Legacy: the RiskAnalizRaporu_T.IO* bool columns)</summary>
public enum ImprovementAction
{
    EliminateAtSource = 1,
    SubstituteWithLessHazardous = 2,
    PreferCollectiveProtection = 3,
    ApplyEngineeringControls = 4,
    UseErgonomicApproaches = 5,
    TrainingAndAwareness = 6,
    WarningAndGuidanceSigns = 7
}

/// <summary>Role of a person taking part in the risk report. (Legacy: CSV string columns)</summary>
public enum ReportParticipantType
{
    WorkerRepresentative = 1,
    SupportStaff = 2,
    KnowledgeableWorker = 3,
    Employer = 4,
    OccupationalSafetySpecialist = 5,
    WorkplacePhysician = 6
}

/// <summary>Type of historical record attached to the risk report. (Legacy: 4 separate tables)</summary>
public enum RiskHistoryRecordType
{
    WorkAccident = 1,
    NoDamageWorkAccident = 2,
    OccupationalDisease = 3,
    NearMissIncident = 4
}

/// <summary>Presence of sensitive worker groups. (Legacy: the KadinCalisan/YasliCalisan/... bool columns)</summary>
public enum VulnerableWorkerGroup
{
    FemaleWorker = 1,
    YoungWorker = 2,
    ElderlyWorker = 3,
    DisabledWorker = 4,
    ChildWorker = 5,
    PregnantOrNursingWorker = 6
}

/// <summary>
/// Which record the identified hazard was derived from.
/// (Legacy: RiskAnalizRaporuBelirlenenTehlike_T.Kaynak string + KaynakId int?)
/// </summary>
public enum HazardSourceType
{
    /// <summary>Entered by hand by the specialist; <c>SourceId</c> is empty.</summary>
    Manual = 0,

    /// <summary>Picked from the hazard library; <c>SourceId</c> = <c>Hazard.Id</c>.</summary>
    HazardLibrary = 1,

    /// <summary>Carried over from a field observation line; <c>SourceId</c> = <c>FieldObservationLine.Id</c>.</summary>
    FieldObservation = 2,

    /// <summary>Carried over from a corrective action record; <c>SourceId</c> = <c>CorrectiveAction.Id</c>.</summary>
    CorrectiveAction = 3,

    /// <summary>Carried over from an incident/accident record; <c>SourceId</c> = <c>Incident.Id</c>.</summary>
    Incident = 4
}

/// <summary>
/// The level a risk score maps to. This is an ordinal scale: the larger the value, the more severe the risk.
/// <para>
/// The thresholds of both the L-Matrix (3x3 / 5x5) and Fine-Kinney methods collapse onto this one scale:
/// <list type="bullet">
/// <item>Fine-Kinney: &lt;20 → <see cref="Negligible"/>, 20-70 → <see cref="Low"/> (possible),
/// 70-200 → <see cref="Medium"/> (substantial), 200-400 → <see cref="High"/>,
/// &gt;400 → <see cref="Intolerable"/> (very high).</item>
/// <item>L-Matrix 5x5: 1-2 → <see cref="Negligible"/>, 3-6 → <see cref="Low"/>,
/// 8-12 → <see cref="Medium"/>, 15-20 → <see cref="High"/>, 25 → <see cref="Intolerable"/>.</item>
/// <item>L-Matrix 3x3: 1 → <see cref="Negligible"/>, 2 → <see cref="Low"/>,
/// 3-4 → <see cref="Medium"/>, 6 → <see cref="High"/>, 9 → <see cref="Intolerable"/>.</item>
/// </list>
/// </para>
/// </summary>
public enum RiskLevel
{
    Unspecified = 0,
    Negligible = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Intolerable = 5
}

// ------------------------------------------------------------------
// Incidents / accidents
// ------------------------------------------------------------------

/// <summary>Incident type. (Legacy: Olay_T.OlayTuru byte)</summary>
public enum IncidentType
{
    WorkAccident = 1,
    NearMiss = 2,
    OccupationalDisease = 3,
    NoInjuryIncident = 4
}

/// <summary>
/// How severe a work accident was.
/// <para>
/// (Legacy: <c>Olay_T.KazaTuru</c>.) The legacy column is named after the accident's type but
/// records its severity: the seven options the form offers are "narrowly avoided", three bands of
/// lost work days, limb loss, disablement and death. That is a different question from
/// <see cref="AccidentType"/>, which asks what happened - a fall, a burn, an electric shock - and
/// answering one with the other would turn "more than three days lost" into "entrapment".
/// </para>
/// <para>
/// The legacy system records no mechanism at all, so <see cref="AccidentType"/> stays
/// <see cref="AccidentType.Unspecified"/> for every migrated incident and this carries what was
/// actually written down.
/// </para>
/// </summary>
public enum AccidentSeverity
{
    Unspecified = 0,

    /// <summary>Hafif atlatilan kaza - no injury of consequence.</summary>
    NarrowlyAvoided = 1,

    /// <summary>3 gun veya daha az is kaybi.</summary>
    UpToThreeLostDays = 2,

    /// <summary>3 gunden fazla is kaybi.</summary>
    MoreThanThreeLostDays = 3,

    /// <summary>Uzuv kaybi.</summary>
    LimbLoss = 4,

    /// <summary>Sakatlanma.</summary>
    Disablement = 5,

    /// <summary>Olum.</summary>
    Fatal = 6,

    /// <summary>Maddi hasar - damage to property, nobody hurt.</summary>
    PropertyDamage = 7
}

/// <summary>Accident type - what physically happened. (No legacy equivalent; see <see cref="AccidentSeverity"/>.)</summary>
public enum AccidentType
{
    Unspecified = 0,
    Fall = 1,
    Impact = 2,
    Entrapment = 3,
    Cut = 4,
    Burn = 5,
    ElectricalShock = 6,
    ChemicalExposure = 7,
    TrafficAccident = 8,
    Poisoning = 9,
    Other = 99
}

/// <summary>Role of a person involved in the incident. (Legacy: OlayKisi_T.KisiTur byte)</summary>
public enum IncidentPersonRole
{
    Affected = 1,
    Witness = 2,
    Responder = 3
}

/// <summary>Emergency team type. (Legacy: AcilDurumEylemPlaniPersoneli_T.EkipTuru string)</summary>
public enum EmergencyTeamType
{
    Unspecified = 0,
    FireFighting = 1,
    RescueAndEvacuation = 2,
    FirstAid = 3,
    Protection = 4,
    Communication = 5
}

/// <summary>
/// A free-text section of the emergency action plan.
/// (Legacy: the plain string columns Icindekiler/Giris/Talimatlar/Savas/... on AcilDurumEylemPlani_T)
/// </summary>
public enum EmergencyPlanSectionType
{
    TableOfContents = 1,
    Introduction = 2,

    /// <summary>Legacy: <c>OrganizasyondaYeralanEkiplerVeSorumluluklari</c>.</summary>
    OrganizationAndResponsibilities = 3,

    Instructions = 4,

    /// <summary>Sabotage/wartime section. Legacy: <c>Savas</c>.</summary>
    Wartime = 5,

    /// <summary>Legacy: <c>AcilDurumTatbikatiUygulamasi</c>.</summary>
    DrillProcedure = 6,

    /// <summary>Legacy: <c>YanginKontrolForumu</c> (a legacy typo; it should read "formu").</summary>
    FireControlForm = 7,

    FirstAid = 8,
    EmergencyPhones = 9
}

// ------------------------------------------------------------------
// Equipment / devices
// ------------------------------------------------------------------

/// <summary>Type of equipment subject to periodic inspection. (Legacy: Cihaz_T.CihazTuru string)</summary>
public enum EquipmentType
{
    Unspecified = 0,
    MachineBench = 1,
    InstallationEquipment = 2,
    LiftingAndConveyingEquipment = 3,
    PressurizedVessel = 4,
    ElectricalInstallation = 5,
    FireSystem = 6
}

// ------------------------------------------------------------------
// Health surveillance
// ------------------------------------------------------------------

/// <summary>Medical examination report type. (Legacy: PeriyodikMuayeneFormu_T.RaporTuru string)</summary>
public enum MedicalReportType
{
    Unspecified = 0,
    PreEmploymentExamination = 1,
    PeriodicExamination = 2,
    JobChange = 3,
    ReturnToWorkExamination = 4,
    OnRequest = 5
}

/// <summary>Fitness-for-work opinion. (Legacy: KanaatVeSonuc string)</summary>
public enum FitnessForWorkOpinion
{
    Unspecified = 0,
    Fit = 1,
    ConditionallyFit = 2,
    Unfit = 3,
    FurtherTestsRequired = 4
}

/// <summary>Complaint headings on the examination form. (Legacy: 20+ separate string columns)</summary>
public enum MedicalComplaintType
{
    ProductiveCough = 1,
    BreathShortness = 2,
    ChestPain = 3,
    Palpitation = 4,
    BackPain = 5,
    DiarrheaOrConstipation = 6,
    JointPain = 7,
    CardiacDisease = 8,
    DiabetesDisease = 9,
    RenalDisease = 10,
    Jaundice = 11,
    GastricOrDuodenalUlcer = 12,
    HearingLoss = 13,
    VisionImpairment = 14,
    NervousSystemDisease = 15,
    SkinDisease = 16,
    FoodPoisoning = 17,
    HospitalAdmission = 18,
    Surgery = 19,
    WorkAccident = 20,
    OccupationalDiseaseSuspicion = 21,
    Disability = 22,
    OngoingTreatment = 23
}

/// <summary>Body system assessed during the physical examination. (Legacy: 12 separate string columns)</summary>
public enum PhysicalExamSystem
{
    SensoryEye = 1,
    SensoryEarNoseThroat = 2,
    SensorySkin = 3,
    CardiovascularSystem = 4,
    RespiratorySystem = 5,
    DigestiveSystem = 6,
    UrogenitalSystem = 7,
    MuscularSkeletalSystem = 8,
    Neurological = 9,
    Psychiatric = 10,
    Other = 99
}

/// <summary>Laboratory test. (Legacy: 8 separate string columns)</summary>
public enum LabTestType
{
    Blood = 1,
    Urine = 2,
    RadiologicalImaging = 3,
    Audiometry = 4,
    RespiratoryFunctionTest = 5,
    PsychologicalTest = 6,
    Other = 99
}

/// <summary>Immunization record. (Legacy: the Tetanoz/Hepatit/Grip/Diger columns)</summary>
public enum ImmunizationType
{
    Tetanus = 1,
    HepatitisA = 2,
    HepatitisB = 3,
    Influenza = 4,
    Covid = 5,
    Other = 99
}

/// <summary>The relative a disease is reported for in the family history. (Legacy: SoyGecmisAnne/Baba/Kardes/Cocuk/Diger)</summary>
public enum FamilyRelation
{
    Mother = 1,
    Father = 2,
    Sibling = 3,
    Child = 4,
    Other = 99
}

/// <summary>Habit type. (Legacy: the Sigara*/Alkol* column group)</summary>
public enum HabitType
{
    Smoking = 1,
    Alcohol = 2,
    Substance = 3
}

/// <summary>Current usage status of a habit.</summary>
public enum HabitStatus
{
    Unspecified = 0,
    NeverUsed = 1,
    Quit = 2,
    CurrentlyUsing = 3
}

/// <summary>
/// The subject of the "is the worker fit to work under this condition?" question on the examination form.
/// (Legacy: <c>PeriyodikMuayeneFormu_T.YuksekCalis</c> / <c>NightCalis</c> /
/// <c>ShiftCalis</c> / <c>WorkCondition</c> / <c>BedenMentally</c> plain string columns)
/// <para>The answer itself is kept in a separate field as a <see cref="TriStateAnswer"/>.</para>
/// </summary>
public enum WorkConditionType
{
    AtHeightWork = 1,
    NightWork = 2,
    ShiftWork = 3,

    /// <summary>Working in heavy and hazardous jobs. (Legacy: <c>CalismaSarti</c>)</summary>
    HeavyAndHazardousWork = 4,

    ConfinedSpaceWork = 5,
    NoisyEnvironment = 6,
    ChemicalExposure = 7,

    /// <summary>
    /// Declaration of overall physical and mental fitness for work. (Legacy: <c>BedenRuhen</c>)
    /// The other members each stand for one specific working condition; this one stands for general fitness.
    /// </summary>
    PhysicalAndMentalFitness = 8
}

// ------------------------------------------------------------------
// E-prescription
// ------------------------------------------------------------------

/// <summary>
/// Note type for an e-prescription or for a prescribed medication.
/// (Legacy: <c>ERecete_T.AciklamaTuru</c> and <c>EPrescriptionMedication_T.MedicationDescriptionType</c> int?;
/// the values were reconstructed from the <c>&lt;select&gt;</c> options in the legacy
/// <c>EPrescription/Views/Erecete/Index.cshtml</c>.)
/// </summary>
public enum PrescriptionNoteType
{
    Unspecified = 0,

    /// <summary>Legacy option value = 1 ("Teşhis/Tanı").</summary>
    Diagnosis = 1,

    /// <summary>Legacy option value = 2 ("Tedavi Süresi") — the legacy default.</summary>
    TreatmentDuration = 2,

    /// <summary>Legacy option value = 3 ("Hasta Güvenlik ve İzleme Formu").</summary>
    PatientSafetyAndMonitoringForm = 3
}

// ------------------------------------------------------------------
// IBYS (the national OHS information management system) integration
// ------------------------------------------------------------------

/// <summary>IBYS submission status. (Legacy: IBYSDurum string "-1"/"0"/"1")</summary>
public enum IbysSubmissionStatus
{
    NotSent = 0,
    Prepared = 1,
    Sent = 2,
    Approved = 3,
    Failed = 4,
    Cancelled = 5
}

/// <summary>IBYS query type. (Legacy: IBYSSorguNo_T.SorguTur string)</summary>
public enum IbysQueryType
{
    Unspecified = 0,
    Training = 1,
    HealthReport = 2,
    ServiceProvidedWorkplace = 3,
    OccupationalSafetySpecialist = 4,
    WorkplacePhysician = 5
}
