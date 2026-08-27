// ---------------------------------------------------------------------------
// GENERATED FILE - do not edit by hand.
//
// Mirrors src/Ensa.Domain.Shared/Enums/*.cs. The API serialises enums as NUMBERS
// (no JsonStringEnumConverter; see EnsaHttpApiModule.ConfigureJson), so the SPA needs the
// numeric values and they must match the backend exactly. Regenerate with:
//
//     python tools/gen-enums/gen_enums.py
//
// Display labels are NOT here: they live in the locale bundles under `enums.*`, because
// the UI ships in Turkish and English.
// ---------------------------------------------------------------------------

/** Invoice type. (Legacy: Faturalar_T.Turu string "Satış"/"Alış") */
export enum InvoiceType {
  Sale = 1,
  Purchase = 2,
  SaleReturn = 3,
  PurchaseReturn = 4,
}

/** Cash register transaction direction. (Legacy: KasaDetay_T.IslemTuru string) */
export enum CashTransactionType {
  Inflow = 1,
  Outflow = 2,
  CarryOver = 3,
}

/** Ledger entry direction. (Legacy: FirmaHareket_T.Borc/Alacak as separate columns) */
export enum LedgerEntryType {
  Debit = 1,
  Credit = 2,
}

/** Payment notification status. (Legacy: Odemeler_T.Durum string) */
export enum PaymentStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Cancelled = 3,
}

/** Service item type. (Legacy: HizmetKartlari_T.KartTuru string) */
export enum ServiceItemType {
  Unspecified = 0,
  OhsService = 1,
  Training = 2,
  HealthScreening = 3,
  Measurement = 4,
  Consultancy = 5,
  Other = 99,
}

/** Contract lifecycle status. (Legacy: SozlesmeDurum string) */
export enum ContractStatus {
  Unspecified = 0,
  InPreparation = 1,
  Sent = 2,
  Signed = 3,
  Rejected = 4,
  Terminated = 5,
}

/** Module reference — where the financial entry originated. (Legacy: Modul string) */
export enum SourceModule {
  Unspecified = 0,
  Invoice = 1,
  CashRegister = 2,
  Collection = 3,
  Expense = 4,
  Contract = 5,
  Manual = 99,
}

/** Visit/appointment activity type. (Legacy: Ziyaret_T.IslemTuru string) */
export enum VisitType {
  Unspecified = 0,
  RoutineVisit = 1,
  FirstVisit = 2,
  FieldObservationVisit = 3,
  Training = 4,
  HealthScreening = 5,
  Measurement = 6,
  Meeting = 7,
  Leave = 8,
  Other = 99,
}

/** Whether the workplace record is a headquarters or a branch. (Legacy: Firma_T.IsYeri string "Merkez"/"Şube") */
export enum WorkplaceType {
  Unspecified = 0,
  Headquarter = 1,
  Branch = 2,
}

/** Company-to-user assignment type. (Legacy: ISGRapor_T.GorevTuru "İçe Grv."/"Dışa Grv.") */
export enum AssignmentType {
  Unspecified = 0,
  InboundAssignment = 1,
  OutboundAssignment = 2,
}

/** Mail delivery status. (Legacy: Mail_T.MailDurumu string) */
export enum MailStatus {
  Draft = 0,
  Queued = 1,
  Sent = 2,
  Failed = 3,
  Cancelled = 4,
}

/** Mail priority level. (Legacy: Mail_T.MailOnemi string) */
export enum MailPriority {
  Low = 0,
  Normal = 1,
  High = 2,
}

/** Mail type. (Legacy: Mail_T.MailTuru string) */
export enum MailType {
  Normal = 0,
  Awareness = 1,
  Reminder = 2,
  System = 3,
}

/** Mail body format. (Legacy: Mail_T.Icerik_Format string) */
export enum ContentFormat {
  PlainText = 0,
  Html = 1,
}

/** The parties taking part in a message exchange. (Legacy: MesajTip) */
export enum MessageType {
  UserMessage = 1,
  EmployeeSenderMessage = 2,
  EmployeeRecipientMessage = 3,
  SystemNotification = 4,
}

/** Support ticket status. (Legacy: UserRequest_T.IsClosed bool) */
export enum SupportTicketStatus {
  Open = 0,
  Answered = 1,
  Closed = 2,
  Cancelled = 3,
}

/** Activity report type. (Legacy: FaliyetRaporu_T.RaporTuru string) */
export enum ActivityReportType {
  Unspecified = 0,
  MonthlyActivityReport = 1,
  AnnualActivityReport = 2,
  PeriodicActivityReport = 3,
  YearEndReviewReport = 4,
}

/** Activity report line type. (Legacy: FaaliyetRaporSatir_T.SatirTuru string) */
export enum ActivityReportLineType {
  OrganizationInfo = 1,
  CompanyInfo = 2,
  Workers = 3,
  BranchCount = 4,
  BranchWorkerCount = 5,
  VisitCount = 6,
  VisitHour = 7,
  TrainedEmployees = 8,
  EmployeesMissingTraining = 9,
  EmployeeHealthReportStatus = 10,
  EquipmentPeriodicInspection = 11,
  UnexaminedEquipments = 12,
  NonConformities = 13,
  Incidents = 14,
  VisitDate = 15,
}

/** Baseline (snapshot) report type. (Legacy: BazalFirmaTablosu.Tur string) */
export enum SnapshotReportType {
  Unspecified = 0,
  CompanySnapshot = 1,
  UserSnapshot = 2,
  OfficeSnapshot = 3,
}

/** The context a field is shown in on the sales rep screen. (Legacy: TemGosterAlan.TemTuru int) */
export enum SalesRepScreenType {
  Unspecified = 0,
  ProspectCompany = 1,
  ContractedCompany = 2,
  Reference = 3,
}

/** Sales rep authority level. (Legacy: Temsilci_T.TemTuru int) */
export enum SalesRepType {
  Unspecified = 0,
  FieldRepresentative = 1,
  RegionOwner = 2,
  Admin = 3,
}

/** Which module record the document is attached to. (Legacy: Arsiv_T.Modul string) */
export enum DocumentOwnerType {
  Unspecified = 0,
  Company = 1,
  CompanyEmployee = 2,
  User = 3,
  WorkPlanLine = 4,
  TrainingPlanLine = 5,
  RiskAssessmentReport = 6,
  FieldObservationReport = 7,
  Equipment = 8,
  WorkplaceDepartment = 9,
  Incident = 10,
  Invoice = 11,
  HealthReport = 12,
  EmergencyActionPlan = 13,
  Bank = 14,
  Office = 15,
  Contract = 16,
}

/** Unit of a period expression. (Legacy: Periyot_T.PeriyotExpression "y1","a6") */
export enum PeriodUnit {
  Day = 1,
  Week = 2,
  Month = 3,
  Year = 4,
}

/** Workplace hazard class as defined by Law 6331. (Legacy: string "AZ TEHLİKELİ" etc.) */
export enum HazardClass {
  Unspecified = 0,
  LowHazard = 1,
  Hazardous = 2,
  VeryHazardous = 3,
}

/** Workplace headcount band. Used by the penalty amount matrix. */
export enum EmployeeCountRange {
  FewerThanTen = 1,
  TenToFortyNine = 2,
  FiftyOrMore = 3,
}

/** The system user's role within the organization. (Legacy: Kullanici_T.PersonelTuru string) */
export enum StaffRole {
  Unspecified = 0,
  OccupationalSafetySpecialist = 1,
  WorkplacePhysician = 2,
  OtherHealthPersonnel = 3,
  OfficeStaff = 4,
  Customer = 5,
  OfficeAdministrator = 6,
  OrganizationAdministrator = 7,
  SystemAdministrator = 8,
}

export enum Gender {
  Unspecified = 0,
  Male = 1,
  Female = 2,
}

export enum MaritalStatus {
  Unspecified = 0,
  Single = 1,
  Married = 2,
  Divorced = 3,
  Widowed = 4,
}

export enum EducationLevel {
  Unspecified = 0,
  NotLiterate = 1,
  Literate = 2,
  PrimarySchool = 3,
  MiddleSchool = 4,
  HighSchool = 5,
  AssociateDegree = 6,
  License = 7,
  MastersDegree = 8,
  Doctorate = 9,
}

export enum BloodType {
  Unspecified = 0,
  ARhPositive = 1,
  ARhNegative = 2,
  BRhPositive = 3,
  BRhNegative = 4,
  ABRhPositive = 5,
  ABRhNegative = 6,
  ZeroRhPositive = 7,
  ZeroRhNegative = 8,
}

/** Three-state yes/no/unknown answer. Used on examination forms instead of free-text strings. */
export enum TriStateAnswer {
  Unspecified = 0,
  No = 1,
  Yes = 2,
  Unknown = 3,
}

/** Normal or pathological examination finding. */
export enum ExamFinding {
  Unspecified = 0,
  Normal = 1,
  Pathological = 2,
  NotPerformed = 3,
}

/** Approval workflow status. (Legacy: OnayDurumu int) */
export enum ApprovalStatus {
  Draft = 0,
  SubmittedForApproval = 1,
  Approved = 2,
  Rejected = 3,
}

/** Permission type. (Legacy: Yetki_T.YetkiTuru "sayfa-yetkisi"/"method-yetkisi") */
export enum PermissionType {
  PagePermission = 1,
  MethodPermission = 2,
  DataPermission = 3,
}

/** Restriction mode that decides which user types a permission may be granted to. (Legacy: Yetki_T.YetkiKisitHedef string — "everybody"/"only-selection"/"except-selected") In and mode, the selected user types are listed in the PermissionRestriction table. */
export enum PermissionRestrictionMode {
  Everyone = 0,
  OnlySelected = 1,
  SelectedExcept = 2,
}

/** Which object the permission is attached to. (Legacy: BaglantiType) */
export enum PermissionScopeType {
  Module = 1,
  UserType = 2,
  Account = 3,
  Menu = 4,
  MenuElement = 5,
}

/** The direction of a per-user menu override. (Legacy: KullaniciMenu_T.IslemTuru string — "added"/"removed") */
export enum UserMenuOverrideAction {
  Added = 1,
  Removed = 2,
}

/** Log record type. (Legacy: Log_T.LogType bool?) */
export enum LogLevel {
  Info = 0,
  Warning = 1,
  Error = 2,
}

/** The action performed on a record (audit log). */
export enum AuditAction {
  Add = 1,
  Update = 2,
  Delete = 3,
  View = 4,
  SignIn = 5,
  SignOut = 6,
}

/** Completion status of a plan line. (Legacy: Durum int, -1/0/1) */
export enum PlanLineStatus {
  Planned = 0,
  Completed = 1,
  NotDone = 2,
  Postponed = 3,
  Cancelled = 4,
}

/** Activity type. (Legacy: Aktivite_T.Tur string) */
export enum ActivityType {
  Activity = 1,
  Document = 2,
  Revision = 3,
  MandatoryDocument = 4,
}

/** Where the training is delivered. (Legacy: EgitimPlaniSatirlari_T.EgitimYeri int) */
export enum TrainingLocation {
  OnSite = 1,
  OffSite = 2,
  RemoteTraining = 3,
}

/** Training type. (Legacy: EgitimPlaniSatirlari_T.EgitimTuru int) */
export enum TrainingType {
  BasicTraining = 1,
  RefresherTraining = 2,
  AdditionalTraining = 3,
}

/** Training subject group. (Legacy: the Egitim_T.GenelKonular/SaglikKonulari/TeknikKonular bool triple) */
export enum TrainingSubjectGroup {
  GeneralSubjects = 1,
  HealthSubjects = 2,
  TechnicalSubjects = 3,
}

/** An action the employee performs during remote training. (Legacy: PersonelIslemEnum) */
export enum EmployeeTrainingAction {
  SignIn = 1,
  FirstTestAttempt = 2,
  FinalTestAttempt = 3,
  TopicProcessing = 4,
  SignOut = 5,
  FirstTestView = 6,
  FinalTestView = 7,
  TopicCompletion = 8,
  TrainingCompletion = 9,
  PasswordChange = 10,
}

/** Exam attempt record type. (Legacy: TestKayitTipi) */
export enum ExamAttemptType {
  FirstTest = 1,
  FinalTest = 2,
}

/** Per-company progression (advance) mode for remote training. (Legacy: FirmaEgitimGecis_T.ManuelGecis string "konu"/"sayfa") */
export enum TrainingProgressMode {
  Topic = 1,
  Page = 2,
}

/** Status of a monthly check (checklist) record belonging to a company. (Legacy: FirmaKontrol_T.Durum / FirmaKontrolSatir_T.Durum string "Aktif" etc.) */
export enum CompanyCheckStatus {
  Unspecified = 0,
  Active = 1,
  Completed = 2,
  Approved = 3,
  Cancelled = 4,
}

/** Indicates which workplace team an employee document belongs to. (Legacy: the FirmaPersonelDosya_T.RiskDegerlendirmeEkibiDosyasi / AcilDurumEkibiDosyasi / IsgKuruluDosyasi bool triple) */
export enum EmployeeTeamDocumentType {
  None = 0,
  RiskAssessmentTeam = 1,
  EmergencyTeam = 2,
  OhsCommittee = 3,
}

/** Risk assessment methodology. (Legacy: RiskAnalizRaporu_T.RaporMetodu string) */
export enum RiskAssessmentMethod {
  Unspecified = 0,
  LMatrixThreeByThree = 1,
  LMatrixFiveByFive = 2,
  FineKinney = 3,
  Fmea = 4,
  Checklist = 5,
}

/** The nature of the risk that was identified. (Legacy: DOF_T.Risk / SahaGozlem Risk string) */
export enum RiskCategory {
  Unspecified = 0,
  WorkAccidentRisk = 1,
  OccupationalDiseaseRisk = 2,
  EnvironmentalRisk = 3,
  FireRisk = 4,
}

/** Outcome of a corrective action. (Legacy: DOF_T.IslemSonucu int; 0/1/-1) */
export enum CorrectiveActionStatus {
  InProgress = 0,
  Closed = 1,
  Cancelled = 2,
}

/** The group exposed to the hazard in a risk assessment. (Legacy: the RiskAnalizRaporu_T.TMK* bool columns) */
export enum ExposedPersonGroup {
  ProductionEmployee = 1,
  MaintenanceEmployee = 2,
  Contractors = 3,
  TechnicalEmployee = 4,
  OfficeStaff = 5,
  AuditEmployee = 6,
  Visitors = 7,
  CleaningEmployee = 8,
  EmergencyEmployee = 9,
  Others = 10,
}

/** An existing protective measure. (Legacy: the RiskAnalizRaporu_T.MKO* bool columns) */
export enum ExistingControlMeasure {
  LocalVentilation = 1,
  MachineGuards = 2,
  PersonalProtectiveUsage = 3,
  FireProtection = 4,
  EmergencyProcedures = 5,
  TrainingAndAwareness = 6,
  WarningSigns = 7,
}

/** A suggested improvement. (Legacy: the RiskAnalizRaporu_T.IO* bool columns) */
export enum ImprovementAction {
  EliminateAtSource = 1,
  SubstituteWithLessHazardous = 2,
  PreferCollectiveProtection = 3,
  ApplyEngineeringControls = 4,
  UseErgonomicApproaches = 5,
  TrainingAndAwareness = 6,
  WarningAndGuidanceSigns = 7,
}

/** Role of a person taking part in the risk report. (Legacy: CSV string columns) */
export enum ReportParticipantType {
  WorkerRepresentative = 1,
  SupportStaff = 2,
  KnowledgeableWorker = 3,
  Employer = 4,
  OccupationalSafetySpecialist = 5,
  WorkplacePhysician = 6,
}

/** Type of historical record attached to the risk report. (Legacy: 4 separate tables) */
export enum RiskHistoryRecordType {
  WorkAccident = 1,
  NoDamageWorkAccident = 2,
  OccupationalDisease = 3,
  NearMissIncident = 4,
}

/** Presence of sensitive worker groups. (Legacy: the KadinCalisan/YasliCalisan/... bool columns) */
export enum VulnerableWorkerGroup {
  FemaleWorker = 1,
  YoungWorker = 2,
  ElderlyWorker = 3,
  DisabledWorker = 4,
  ChildWorker = 5,
  PregnantOrNursingWorker = 6,
}

/** Which record the identified hazard was derived from. (Legacy: RiskAnalizRaporuBelirlenenTehlike_T.Kaynak string + KaynakId int?) */
export enum HazardSourceType {
  Manual = 0,
  HazardLibrary = 1,
  FieldObservation = 2,
  CorrectiveAction = 3,
  Incident = 4,
}

/** The level a risk score maps to. This is an ordinal scale: the larger the value, the more severe the risk. The thresholds of both the L-Matrix (3x3 / 5x5) and Fine-Kinney methods collapse onto this one scale: Fine-Kinney: &lt;20 → , 20-70 → (possible), 70-200 → (substantial), 200-400 → , &gt;400 → (very high). L-Matrix 5x5: 1-2 → , 3-6 → , 8-12 → , 15-20 → , 25 → . L-Matrix 3x3: 1 → , 2 → , 3-4 → , 6 → , 9 → . */
export enum RiskLevel {
  Unspecified = 0,
  Negligible = 1,
  Low = 2,
  Medium = 3,
  High = 4,
  Intolerable = 5,
}

/** Incident type. (Legacy: Olay_T.OlayTuru byte) */
export enum IncidentType {
  WorkAccident = 1,
  NearMiss = 2,
  OccupationalDisease = 3,
  NoInjuryIncident = 4,
}

/** How severe a work accident was. (Legacy: Olay_T.KazaTuru.) The legacy column is named after the accident's type but records its severity: the seven options the form offers are "narrowly avoided", three bands of lost work days, limb loss, disablement and death. That is a different question from , which asks what happened - a fall, a burn, an electric shock - and answering one with the other would turn "more than three days lost" into "entrapment". The legacy system records no mechanism at all, so stays for every migrated incident and this carries what was actually written down. */
export enum AccidentSeverity {
  Unspecified = 0,
  NarrowlyAvoided = 1,
  UpToThreeLostDays = 2,
  MoreThanThreeLostDays = 3,
  LimbLoss = 4,
  Disablement = 5,
  Fatal = 6,
  PropertyDamage = 7,
}

/** Accident type - what physically happened. (No legacy equivalent; see .) */
export enum AccidentType {
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
  Other = 99,
}

/** Role of a person involved in the incident. (Legacy: OlayKisi_T.KisiTur byte) */
export enum IncidentPersonRole {
  Affected = 1,
  Witness = 2,
  Responder = 3,
}

/** Emergency team type. (Legacy: AcilDurumEylemPlaniPersoneli_T.EkipTuru string) */
export enum EmergencyTeamType {
  Unspecified = 0,
  FireFighting = 1,
  RescueAndEvacuation = 2,
  FirstAid = 3,
  Protection = 4,
  Communication = 5,
}

/** A free-text section of the emergency action plan. (Legacy: the plain string columns Icindekiler/Giris/Talimatlar/Savas/... on AcilDurumEylemPlani_T) */
export enum EmergencyPlanSectionType {
  TableOfContents = 1,
  Introduction = 2,
  OrganizationAndResponsibilities = 3,
  Instructions = 4,
  Wartime = 5,
  DrillProcedure = 6,
  FireControlForm = 7,
  FirstAid = 8,
  EmergencyPhones = 9,
}

/** Type of equipment subject to periodic inspection. (Legacy: Cihaz_T.CihazTuru string) */
export enum EquipmentType {
  Unspecified = 0,
  MachineBench = 1,
  InstallationEquipment = 2,
  LiftingAndConveyingEquipment = 3,
  PressurizedVessel = 4,
  ElectricalInstallation = 5,
  FireSystem = 6,
}

/** Medical examination report type. (Legacy: PeriyodikMuayeneFormu_T.RaporTuru string) */
export enum MedicalReportType {
  Unspecified = 0,
  PreEmploymentExamination = 1,
  PeriodicExamination = 2,
  JobChange = 3,
  ReturnToWorkExamination = 4,
  OnRequest = 5,
}

/** Fitness-for-work opinion. (Legacy: KanaatVeSonuc string) */
export enum FitnessForWorkOpinion {
  Unspecified = 0,
  Fit = 1,
  ConditionallyFit = 2,
  Unfit = 3,
  FurtherTestsRequired = 4,
}

/** Complaint headings on the examination form. (Legacy: 20+ separate string columns) */
export enum MedicalComplaintType {
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
  OngoingTreatment = 23,
}

/** Body system assessed during the physical examination. (Legacy: 12 separate string columns) */
export enum PhysicalExamSystem {
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
  Other = 99,
}

/** Laboratory test. (Legacy: 8 separate string columns) */
export enum LabTestType {
  Blood = 1,
  Urine = 2,
  RadiologicalImaging = 3,
  Audiometry = 4,
  RespiratoryFunctionTest = 5,
  PsychologicalTest = 6,
  Other = 99,
}

/** Immunization record. (Legacy: the Tetanoz/Hepatit/Grip/Diger columns) */
export enum ImmunizationType {
  Tetanus = 1,
  HepatitisA = 2,
  HepatitisB = 3,
  Influenza = 4,
  Covid = 5,
  Other = 99,
}

/** The relative a disease is reported for in the family history. (Legacy: SoyGecmisAnne/Baba/Kardes/Cocuk/Diger) */
export enum FamilyRelation {
  Mother = 1,
  Father = 2,
  Sibling = 3,
  Child = 4,
  Other = 99,
}

/** Habit type. (Legacy: the Sigara* /Alkol* column group) */
export enum HabitType {
  Smoking = 1,
  Alcohol = 2,
  Substance = 3,
}

/** Current usage status of a habit. */
export enum HabitStatus {
  Unspecified = 0,
  NeverUsed = 1,
  Quit = 2,
  CurrentlyUsing = 3,
}

/** The subject of the "is the worker fit to work under this condition?" question on the examination form. (Legacy: PeriyodikMuayeneFormu_T.YuksekCalis / NightCalis / ShiftCalis / WorkCondition / BedenMentally plain string columns) The answer itself is kept in a separate field as a . */
export enum WorkConditionType {
  AtHeightWork = 1,
  NightWork = 2,
  ShiftWork = 3,
  HeavyAndHazardousWork = 4,
  ConfinedSpaceWork = 5,
  NoisyEnvironment = 6,
  ChemicalExposure = 7,
  PhysicalAndMentalFitness = 8,
}

/** Note type for an e-prescription or for a prescribed medication. (Legacy: ERecete_T.AciklamaTuru and EPrescriptionMedication_T.MedicationDescriptionType int?; the values were reconstructed from the &lt;select&gt; options in the legacy EPrescription/Views/Erecete/Index.cshtml.) */
export enum PrescriptionNoteType {
  Unspecified = 0,
  Diagnosis = 1,
  TreatmentDuration = 2,
  PatientSafetyAndMonitoringForm = 3,
}

/** IBYS submission status. (Legacy: IBYSDurum string "-1"/"0"/"1") */
export enum IbysSubmissionStatus {
  NotSent = 0,
  Prepared = 1,
  Sent = 2,
  Approved = 3,
  Failed = 4,
  Cancelled = 5,
}

/** IBYS query type. (Legacy: IBYSSorguNo_T.SorguTur string) */
export enum IbysQueryType {
  Unspecified = 0,
  Training = 1,
  HealthReport = 2,
  ServiceProvidedWorkplace = 3,
  OccupationalSafetySpecialist = 4,
  WorkplacePhysician = 5,
}
