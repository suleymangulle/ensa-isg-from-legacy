using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// A signed subscription contract — the formal record that follows the
/// <see cref="ProspectOrganization"/> stage.
/// Legacy: <c>SozlesmeliFirmalar_T</c>.
/// <para>
/// A host record, owned by sales and accounting, so it does NOT implement <see cref="IMultiTenant"/>.
/// Legacy held <c>Package</c> and <c>OrganizationType</c> as string codes; both were normalized
/// into FKs.
/// </para>
/// </summary>
public class OrganizationContract : FullAuditedEntity
{
    /// <summary>FK to the tenant the contract belongs to. (Legacy: FirmaId — the same value as KurumId.)</summary>
    public int OrganizationId { get; set; }

    /// <summary>The organization's name at the time of signing — a historical copy. (Legacy: FirmaAdi)</summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>(Legacy: YetkiliTcNo — stored encrypted via <c>[EncryptColumn]</c>; it is still stored encrypted.)</summary>
    public string? AuthorizedNationalId { get; set; }

    /// <summary>(Legacy: YetkiliAdi)</summary>
    public string? AuthorizedName { get; set; }

    /// <summary>(Legacy: YetkiliSoyAdi)</summary>
    public string? AuthorizedLastName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    /// <summary>(Legacy: SozlesmeTarihi)</summary>
    public DateTime ContractDate { get; set; }

    /// <summary>Unit price per user.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>The number of users covered by the contract.</summary>
    public int UserCount { get; set; }

    public decimal TotalPrice { get; set; }

    /// <summary>FK to the purchased subscription plan. (Legacy: the Paket string code)</summary>
    public int? SubscriptionPlanId { get; set; }

    /// <summary>FK to the organization type. (Legacy: the KurumTuru string code)</summary>
    public int? OrganizationTypeId { get; set; }

    /// <summary>Whether the contract has been approved. (Legacy: Onay)</summary>
    public bool IsApproved { get; set; }

    /// <summary>(Legacy: Odendi)</summary>
    public bool IsPaid { get; set; }

    /// <summary>FK to the sales rep who signed the contract. (Legacy: TemsilciId)</summary>
    public int? SalesRepId { get; set; }

    /// <summary>FK to the referring company. (Legacy: ReferansId)</summary>
    public int? ReferenceCompanyId { get; set; }

    /// <summary>FK to the sales rep assignment log record. (Legacy: AtamaLogu)</summary>
    public int? AssignmentLogId { get; set; }

    /// <summary>Whether the contract is active. (Legacy: Durum bool? — null also counted as active.)</summary>
    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }

    /// <summary>(Legacy: SozlesmeDurum string)</summary>
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Unspecified;

    public string? ContractNote { get; set; }

    public DateTime? ContractStatusDate { get; set; }

    /// <summary>The date the account was closed; when set, the subscription has ended.</summary>
    public DateTime? AccountClosingDate { get; set; }
}
