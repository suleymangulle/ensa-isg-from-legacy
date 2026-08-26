namespace Ensa.Domain.Shared.Enums;

// ------------------------------------------------------------------
// Finance
// ------------------------------------------------------------------

/// <summary>Invoice type. (Legacy: Faturalar_T.Turu string "Satış"/"Alış")</summary>
public enum InvoiceType
{
    Sale = 1,
    Purchase = 2,
    SaleReturn = 3,
    PurchaseReturn = 4
}

/// <summary>Cash register transaction direction. (Legacy: KasaDetay_T.IslemTuru string)</summary>
public enum CashTransactionType
{
    Inflow = 1,
    Outflow = 2,
    CarryOver = 3
}

/// <summary>Ledger entry direction. (Legacy: FirmaHareket_T.Borc/Alacak as separate columns)</summary>
public enum LedgerEntryType
{
    Debit = 1,
    Credit = 2
}

/// <summary>Payment notification status. (Legacy: Odemeler_T.Durum string)</summary>
public enum PaymentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

/// <summary>Service item type. (Legacy: HizmetKartlari_T.KartTuru string)</summary>
public enum ServiceItemType
{
    Unspecified = 0,
    OhsService = 1,
    Training = 2,
    HealthScreening = 3,
    Measurement = 4,
    Consultancy = 5,
    Other = 99
}

/// <summary>Contract lifecycle status. (Legacy: SozlesmeDurum string)</summary>
public enum ContractStatus
{
    Unspecified = 0,
    InPreparation = 1,
    Sent = 2,
    Signed = 3,
    Rejected = 4,
    Terminated = 5
}

/// <summary>Module reference — where the financial entry originated. (Legacy: Modul string)</summary>
public enum SourceModule
{
    Unspecified = 0,
    Invoice = 1,
    CashRegister = 2,
    Collection = 3,
    Expense = 4,
    Contract = 5,
    Manual = 99
}

// ------------------------------------------------------------------
// CRM / Visits
// ------------------------------------------------------------------

/// <summary>Visit/appointment activity type. (Legacy: Ziyaret_T.IslemTuru string)</summary>
public enum VisitType
{
    Unspecified = 0,
    RoutineVisit = 1,
    FirstVisit = 2,
    FieldObservationVisit = 3,
    Training = 4,
    HealthScreening = 5,
    Measurement = 6,
    Meeting = 7,
    Leave = 8,
    Other = 99
}

/// <summary>
/// Whether the workplace record is a headquarters or a branch.
/// (Legacy: Firma_T.IsYeri string "Merkez"/"Şube")
/// </summary>
public enum WorkplaceType
{
    Unspecified = 0,
    Headquarter = 1,
    Branch = 2
}

/// <summary>Company-to-user assignment type. (Legacy: ISGRapor_T.GorevTuru "İçe Grv."/"Dışa Grv.")</summary>
public enum AssignmentType
{
    Unspecified = 0,
    InboundAssignment = 1,
    OutboundAssignment = 2
}

// ------------------------------------------------------------------
// Communication
// ------------------------------------------------------------------

/// <summary>Mail delivery status. (Legacy: Mail_T.MailDurumu string)</summary>
public enum MailStatus
{
    Draft = 0,
    Queued = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>Mail priority level. (Legacy: Mail_T.MailOnemi string)</summary>
public enum MailPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

/// <summary>Mail type. (Legacy: Mail_T.MailTuru string)</summary>
public enum MailType
{
    Normal = 0,
    Awareness = 1,
    Reminder = 2,
    System = 3
}

/// <summary>Mail body format. (Legacy: Mail_T.Icerik_Format string)</summary>
public enum ContentFormat
{
    PlainText = 0,
    Html = 1
}

/// <summary>The parties taking part in a message exchange. (Legacy: MesajTip)</summary>
public enum MessageType
{
    UserMessage = 1,
    EmployeeSenderMessage = 2,
    EmployeeRecipientMessage = 3,
    SystemNotification = 4
}

/// <summary>Support ticket status. (Legacy: UserRequest_T.IsClosed bool)</summary>
public enum SupportTicketStatus
{
    Open = 0,
    Answered = 1,
    Closed = 2,
    Cancelled = 3
}

// ------------------------------------------------------------------
// Reporting
// ------------------------------------------------------------------

/// <summary>Activity report type. (Legacy: FaliyetRaporu_T.RaporTuru string)</summary>
public enum ActivityReportType
{
    Unspecified = 0,
    MonthlyActivityReport = 1,
    AnnualActivityReport = 2,
    PeriodicActivityReport = 3,
    YearEndReviewReport = 4
}

/// <summary>Activity report line type. (Legacy: FaaliyetRaporSatir_T.SatirTuru string)</summary>
public enum ActivityReportLineType
{
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
    Incidents = 14
}

/// <summary>Baseline (snapshot) report type. (Legacy: BazalFirmaTablosu.Tur string)</summary>
public enum SnapshotReportType
{
    Unspecified = 0,
    CompanySnapshot = 1,
    UserSnapshot = 2,
    OfficeSnapshot = 3
}

/// <summary>The context a field is shown in on the sales rep screen. (Legacy: TemGosterAlan.TemTuru int)</summary>
public enum SalesRepScreenType
{
    Unspecified = 0,
    ProspectCompany = 1,
    ContractedCompany = 2,
    Reference = 3
}

/// <summary>Sales rep authority level. (Legacy: Temsilci_T.TemTuru int)</summary>
public enum SalesRepType
{
    Unspecified = 0,
    FieldRepresentative = 1,
    RegionOwner = 2,
    Admin = 3
}

// ------------------------------------------------------------------
// Documents
// ------------------------------------------------------------------

/// <summary>Which module record the document is attached to. (Legacy: Arsiv_T.Modul string)</summary>
public enum DocumentOwnerType
{
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
    Contract = 16
}

/// <summary>Unit of a period expression. (Legacy: Periyot_T.PeriyotExpression "y1","a6")</summary>
public enum PeriodUnit
{
    Day = 1,
    Week = 2,
    Month = 3,
    Year = 4
}
