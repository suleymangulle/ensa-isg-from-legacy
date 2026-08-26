namespace Ensa.Domain.Shared.Enums;

/// <summary>Workplace hazard class as defined by Law 6331. (Legacy: string "AZ TEHLİKELİ" etc.)</summary>
public enum HazardClass
{
    Unspecified = 0,
    LowHazard = 1,
    Hazardous = 2,
    VeryHazardous = 3
}

/// <summary>Workplace headcount band. Used by the penalty amount matrix.</summary>
public enum EmployeeCountRange
{
    FewerThanTen = 1,
    TenToFortyNine = 2,
    FiftyOrMore = 3
}

/// <summary>The system user's role within the organization. (Legacy: Kullanici_T.PersonelTuru string)</summary>
public enum StaffRole
{
    Unspecified = 0,
    OccupationalSafetySpecialist = 1,
    WorkplacePhysician = 2,
    OtherHealthPersonnel = 3,
    OfficeStaff = 4,
    Customer = 5,
    OfficeAdministrator = 6,
    OrganizationAdministrator = 7,
    SystemAdministrator = 8
}

public enum Gender
{
    Unspecified = 0,
    Male = 1,
    Female = 2
}

public enum MaritalStatus
{
    Unspecified = 0,
    Single = 1,
    Married = 2,
    Divorced = 3,
    Widowed = 4
}

public enum EducationLevel
{
    Unspecified = 0,
    NotLiterate = 1,
    Literate = 2,
    PrimarySchool = 3,
    MiddleSchool = 4,
    HighSchool = 5,
    AssociateDegree = 6,
    License = 7,
    MastersDegree = 8,
    Doctorate = 9
}

public enum BloodType
{
    Unspecified = 0,
    ARhPositive = 1,
    ARhNegative = 2,
    BRhPositive = 3,
    BRhNegative = 4,
    ABRhPositive = 5,
    ABRhNegative = 6,
    ZeroRhPositive = 7,
    ZeroRhNegative = 8
}

/// <summary>Three-state yes/no/unknown answer. Used on examination forms instead of free-text strings.</summary>
public enum TriStateAnswer
{
    Unspecified = 0,
    No = 1,
    Yes = 2,
    Unknown = 3
}

/// <summary>Normal or pathological examination finding.</summary>
public enum ExamFinding
{
    Unspecified = 0,
    Normal = 1,
    Pathological = 2,
    NotPerformed = 3
}

/// <summary>Approval workflow status. (Legacy: OnayDurumu int)</summary>
public enum ApprovalStatus
{
    Draft = 0,
    SubmittedForApproval = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>Permission type. (Legacy: Yetki_T.YetkiTuru "sayfa-yetkisi"/"method-yetkisi")</summary>
public enum PermissionType
{
    PagePermission = 1,
    MethodPermission = 2,
    DataPermission = 3
}

/// <summary>
/// Restriction mode that decides which user types a permission may be granted to.
/// (Legacy: Yetki_T.YetkiKisitHedef string — "everybody"/"only-selection"/"except-selected")
/// <para>
/// In <see cref="OnlySelected"/> and <see cref="SelectedExcept"/> mode, the selected
/// user types are listed in the <c>PermissionRestriction</c> table.
/// </para>
/// </summary>
public enum PermissionRestrictionMode
{
    /// <summary>No restriction; the permission may be granted to any user type. (Legacy: "everybody")</summary>
    Everyone = 0,

    /// <summary>The permission may be granted ONLY to the user types in the restriction list. (Legacy: "only-selection")</summary>
    OnlySelected = 1,

    /// <summary>The permission may be granted to every user type EXCEPT those in the restriction list. (Legacy: "except-selected")</summary>
    SelectedExcept = 2
}

/// <summary>Which object the permission is attached to. (Legacy: BaglantiType)</summary>
public enum PermissionScopeType
{
    Module = 1,
    UserType = 2,
    Account = 3,
    Menu = 4,
    MenuElement = 5
}

/// <summary>
/// The direction of a per-user menu override.
/// (Legacy: KullaniciMenu_T.IslemTuru string — "added"/"removed")
/// </summary>
public enum UserMenuOverrideAction
{
    /// <summary>An item that is not in the default menu is additionally shown to this user. (Legacy: "added")</summary>
    Added = 1,

    /// <summary>An item that is in the default menu is hidden from this user. (Legacy: "removed")</summary>
    Removed = 2
}

/// <summary>Log record type. (Legacy: Log_T.LogType bool?)</summary>
public enum LogLevel
{
    Info = 0,
    Warning = 1,
    Error = 2
}

/// <summary>The action performed on a record (audit log).</summary>
public enum AuditAction
{
    Add = 1,
    Update = 2,
    Delete = 3,
    View = 4,
    SignIn = 5,
    SignOut = 6
}
