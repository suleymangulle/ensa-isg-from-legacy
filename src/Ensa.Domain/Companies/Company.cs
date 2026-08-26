using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// Client workplace (company). This is the core record of the domain; nearly every OHS process
/// (training, health surveillance, risk assessment, visits, the ledger) hangs off it.
/// <para>Legacy equivalent: <c>Company_T</c>.</para>
/// <para>
/// The headquarter/branch relationship is a self-reference through
/// <see cref="HeadquarterCompanyId"/>; navigation properties are NOT used — combined reads go
/// through <c>CompanyNavigation</c>.
/// </para>
/// </summary>
public class Company : FullAuditedTenantEntity, IActivatable, ICompanyRecord
{
    // ---------------- Identity ----------------

    /// <summary>Legal name of the workplace (company).</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Legacy synchronisation/external system key. (Legacy: <c>Firma_T.SID</c>)
    /// </summary>
    public string? Sid { get; set; }

    /// <summary>SSI workplace registration number (stored normalised, without spaces).</summary>
    public string? SsiNumber { get; set; }

    public string? TaxTaxOffice { get; set; }

    public string? TaxNumber { get; set; }

    /// <summary>Full name of the employer. (Legacy: <c>IsVeren</c>)</summary>
    public string? EmployerName { get; set; }

    /// <summary>Employer's mobile phone. (Legacy: <c>IsVerenGSM</c>)</summary>
    public string? EmployerMobilePhone { get; set; }

    /// <summary>
    /// Free-text NACE / field of activity. (Legacy: <c>FaaliyetAlani</c>)
    /// Its normalised counterpart is <see cref="OccupationCodeId"/>; both are kept.
    /// </summary>
    public string? BusinessActivity { get; set; }

    /// <summary>NACE/occupation code reference (host table <c>OccupationCode</c>).</summary>
    public int? OccupationCodeId { get; set; }

    /// <summary>Workplace hazard class under Turkish OHS law no. 6331. (Legacy: string)</summary>
    public HazardClass HazardClass { get; set; } = HazardClass.Unspecified;

    /// <summary>Whether the user has confirmed the hazard class derived from the NACE code.</summary>
    public bool OrganizationTypeVerified { get; set; }

    /// <summary>Organization type reference. (Legacy: <c>KurumTuru</c> string)</summary>
    public int? OrganizationTypeId { get; set; }

    /// <summary>Service plan reference. (Legacy: <c>PaketTuru</c> string)</summary>
    public int? SubscriptionPlanId { get; set; }

    /// <summary>
    /// When <c>true</c>, this record is not a client workplace but the service provider's
    /// (tenant's) own workplace record; it is excluded from client lists and counts.
    /// (Legacy: <c>Firma_T.Kurum</c> bool)
    /// </summary>
    public bool IsOrganizationRecord { get; set; }

    /// <summary>Whether the workplace is a subcontractor site.</summary>
    public bool? IsSubcontractor { get; set; }

    /// <summary>Marks the workplace as a solution partner.</summary>
    public bool SolutionPartner { get; set; }

    // ---------------- Headquarter / branch ----------------

    /// <summary>Whether the workplace is a headquarter or a branch. (Legacy: <c>IsYeri</c> string)</summary>
    public WorkplaceType WorkplaceType { get; set; } = WorkplaceType.Unspecified;

    /// <summary>
    /// For a branch, the id of the headquarter company it belongs to (self-referencing FK).
    /// (Legacy: <c>MerkezId</c>) There is NO navigation property.
    /// </summary>
    public int? HeadquarterCompanyId { get; set; }

    /// <summary>Branch number.</summary>
    public int? BranchNo { get; set; }

    /// <summary>Branch name (to distinguish it from the headquarter).</summary>
    public string? BranchName { get; set; }

    public string? BranchContact { get; set; }

    /// <summary>Mobile phone of the branch contact. (Legacy: <c>SubeYetkilisiGSM</c>)</summary>
    public string? BranchContactGsm { get; set; }

    /// <summary>Holding/group company reference.</summary>
    public int? GroupCorporateId { get; set; }

    // ---------------- Contact / address ----------------

    public string? Address { get; set; }

    /// <summary>Invoice address, when it differs from the main address.</summary>
    public string? InvoiceAddress { get; set; }

    public int CityId { get; set; }

    public int DistrictId { get; set; }

    public int? QuarterId { get; set; }

    public int? NeighborhoodId { get; set; }

    /// <summary>Geographic latitude. (Legacy: the first part of the <c>LatLng</c> string)</summary>
    public decimal? Latitude { get; set; }

    /// <summary>Geographic longitude. (Legacy: the second part of the <c>LatLng</c> string)</summary>
    public decimal? Longitude { get; set; }

    public string? Phone { get; set; }

    public string? Fax { get; set; }

    /// <summary>Mobile phone. (Legacy: <c>GSM</c>)</summary>
    public string? Gsm { get; set; }

    public string? Email { get; set; }

    /// <summary>CC address list used on notification e-mails (semicolon separated).</summary>
    public string? Cc { get; set; }

    public string? WebUrl { get; set; }

    public string? AuthorizedPerson { get; set; }

    public string? AuthorizedPersonPhone { get; set; }

    public string? AuthorizedPersonEmail { get; set; }

    public string? FinanceOwner { get; set; }

    /// <summary>Mobile phone of the finance contact. (Legacy: <c>FinansSorumlusuGSM</c>)</summary>
    public string? FinanceOwnerGsm { get; set; }

    // ---------------- Service / operations ----------------

    /// <summary>Reference to the office (branch) that provides the service.</summary>
    public int OfficeId { get; set; }

    /// <summary>Operational region code.</summary>
    public int? RegionCode { get; set; }

    /// <summary>Ordering priority in lists.</summary>
    public int? Priority { get; set; }

    /// <summary>Monthly visit time of the OHS specialist, in minutes.</summary>
    public int? VisitSpecialist { get; set; }

    /// <summary>Monthly visit time of the workplace physician, in minutes.</summary>
    public int? VisitPhysician { get; set; }

    /// <summary>Number of workers reported through İSG-KATİP. (Legacy: <c>ISGKatipCalisanSayisi</c>)</summary>
    public int? OhsKatipWorkerCount { get; set; }

    /// <summary>Whether to include the workplace in the first month's service programme.</summary>
    public bool FirstMonthProgramIncluded { get; set; }

    /// <summary>Whether the user-count limit applies to distance learning.</summary>
    public bool? UserLimit { get; set; }

    /// <summary>Whether distance-learning credentials have been sent to the company's employees. (Legacy: <c>SifreGonderildi</c> int?)</summary>
    public bool PasswordSent { get; set; }

    // ---------------- Financial ----------------

    /// <summary>Official (invoiced) monthly service fee.</summary>
    public decimal? MonthlyFeeOfficial { get; set; }

    /// <summary>Total monthly service fee (official + unofficial).</summary>
    public decimal? MonthlyFeeTotal { get; set; }

    /// <summary>Service fee of the OHS specialist.</summary>
    public decimal? SpecialistFee { get; set; }

    /// <summary>Service fee of the workplace physician.</summary>
    public decimal? PhysicianFee { get; set; }

    /// <summary>Official invoice amount.</summary>
    public decimal? InvoiceAmount { get; set; }

    /// <summary>Off-the-books (unofficial) invoice amount. (Legacy: <c>FaturaTutariKh</c>)</summary>
    public decimal? InvoiceAmountKh { get; set; }

    /// <summary>Group/master contract amount. (Legacy: <c>GRSozlesmeTutari</c>)</summary>
    public decimal? GrContractAmount { get; set; }

    /// <summary>Amount expected to be paid according to the ledger.</summary>
    public decimal? PayableDigit { get; set; }

    /// <summary>Expected/planned payment date.</summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>Whether the quoted amount includes VAT. (Legacy: string)</summary>
    public bool QuoteVatIncluded { get; set; }

    /// <summary>Whether unofficial amounts are shown on screen. (Legacy: <c>GayriRTGosterilsinMi</c>)</summary>
    public bool ShowUnofficialAmount { get; set; }

    // ---------------- Notes / imagery ----------------

    /// <summary>Free-text note.</summary>
    public string? Notes { get; set; }

    /// <summary>Note shown as a warning on screen.</summary>
    public string? WarningNote { get; set; }

    /// <summary>Name of the person who recorded the note (legacy free text).</summary>
    public string? NoteRecordedBy { get; set; }

    /// <summary>
    /// Company logo — FK to the central <c>Document</c> table.
    /// (Legacy: <c>FirmaLogo</c> byte[] + <c>CompanyLogoDocumentName</c> + <c>CompanyLogoDocumentType</c>)
    /// </summary>
    public int? LogoDocumentId { get; set; }

    // ---------------- Status ----------------

    /// <summary>Whether the record is active. (Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
