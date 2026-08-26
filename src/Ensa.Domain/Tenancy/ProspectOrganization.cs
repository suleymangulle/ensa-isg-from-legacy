using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// A prospective customer — a sales lead that has not become a tenant yet.
/// Legacy: <c>CustomerPackage_T</c>.
/// <para>
/// A host CRM record: it belongs to no tenant, so it does NOT implement <see cref="IMultiTenant"/>.
/// Once the contract is signed and the account opened, <see cref="OrganizationId"/> links it to the
/// tenant that was created.
/// </para>
/// <para>
/// The legacy <c>Password</c> column was NOT carried over: when the account is opened the user is
/// created through ASP.NET Core Identity and the password is stored in <c>User.PasswordHash</c>.
/// </para>
/// </summary>
public class ProspectOrganization : FullAuditedEntity
{
    /// <summary>(Legacy: Name)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>(Legacy: Surname)</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>(Legacy: TCKN — it was not an encrypted column, even though it is personal data.)</summary>
    public string? NationalId { get; set; }

    /// <summary>Trading name of the OHS service provider. (Legacy: OSGBTitle)</summary>
    public string? OrganizationTitle { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    /// <summary>Whether the application is from an individual rather than a legal entity. (Legacy: IsIndividual bool?)</summary>
    public bool IsIndividual { get; set; }

    /// <summary>Whether the prospect is an OHS service provider (OSGB). (Legacy: IsOSGB bool?)</summary>
    public bool IsOhsProvider { get; set; }

    /// <summary>Whether the prospect employs a workplace physician. (Legacy: IsDoctor bool?)</summary>
    public bool PhysicianExists { get; set; }

    /// <summary>The number of OHS specialists requested. (Legacy: NumberOfSpecialist)</summary>
    public int? SpecialistCount { get; set; }

    /// <summary>FK to the requested subscription plan. (Legacy: PackageType int? — it pointed at PaketTuru_T.)</summary>
    public int? SubscriptionPlanId { get; set; }

    /// <summary>Quoted amount, excluding VAT. (Legacy: Price double → decimal)</summary>
    public decimal? Price { get; set; }

    /// <summary>VAT rate, as a percentage. (Legacy: KDV double → decimal)</summary>
    public decimal? VatRate { get; set; }

    /// <summary>KDV dahil toplam tutar. (Legacy: KDVPrice double → decimal)</summary>
    public decimal? GrossWithVatPrice { get; set; }

    /// <summary>(Legacy: IsPaid bool?)</summary>
    public bool Paid { get; set; }

    /// <summary>Whether a demo account was requested. (Legacy: IsDemo bool?)</summary>
    public bool IsDemo { get; set; }

    /// <summary>Whether the information or quotation e-mail has been sent. (Legacy: IsMailSent bool?)</summary>
    public bool MailSent { get; set; }

    /// <summary>Registration date — a business date kept separately from <c>CreationTime</c>. (Legacy: RegistrationDate)</summary>
    public DateTime? RecordDate { get; set; }

    /// <summary>FK to the tenant created when the prospect's account was opened. (Legacy: FirmaId)</summary>
    public int? OrganizationId { get; set; }

    /// <summary>FK to the sales rep following the prospect. (Legacy: TemsilciId)</summary>
    public int? SalesRepId { get; set; }

    /// <summary>FK to the company that referred the prospect. (Legacy: ReferansId — it pointed at Firma_T.)</summary>
    public int? ReferenceCompanyId { get; set; }

    /// <summary>FK to the sales rep assignment log record. (Legacy: AtamaLogu)</summary>
    public int? AssignmentLogId { get; set; }

    /// <summary>Whether the prospect record is active. (Legacy: Durum bool? — null also counted as active.)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Free-text sales note. (Legacy: Not)</summary>
    public string? Note { get; set; }

    /// <summary>(Legacy: SozlesmeDurum string)</summary>
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Unspecified;

    public string? ContractNote { get; set; }

    public DateTime? ContractStatusDate { get; set; }
}
