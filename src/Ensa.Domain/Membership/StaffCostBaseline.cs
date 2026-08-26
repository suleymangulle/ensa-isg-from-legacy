using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Membership;

/// <summary>
/// Staff cost baseline record — a snapshot of a user's personnel cost and İSG-KATİP minute
/// capacity for a given period.
/// Legacy: <c>BazalKullanici_T</c> (its tenant column was <c>OrganizationId</c>).
/// <para>
/// Because this is a snapshot, fields such as the name and the staff role are not read from the
/// live <see cref="User"/> record; they are COPIED with the values they had at the time the
/// record was taken.
/// </para>
/// </summary>
public class StaffCostBaseline : FullAuditedTenantEntity, IActivatable
{
    /// <summary>FK of the related live user. (Legacy: IliskiliKullaniciId)</summary>
    public int? UserId { get; set; }

    public int OfficeId { get; set; }

    /// <summary>Full name at the time of the snapshot. (Legacy: AdSoyad)</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Staff role at the time of the snapshot. (Legacy: PersonelTuru string)</summary>
    public StaffRole StaffRole { get; set; } = StaffRole.Unspecified;

    public DateTime? HireDate { get; set; }

    /// <summary>Salary for the period. (Legacy: Maas decimal)</summary>
    public decimal Salary { get; set; }

    /// <summary>Employer's SSI cost for the period. (Legacy: SGKTutari decimal)</summary>
    public decimal SsiAmount { get; set; }

    /// <summary>Number of days worked in the period. (Legacy: CalisilanGun int?)</summary>
    public int? WorkedDayCount { get; set; }

    /// <summary>Total assignable minutes registered in İSG-KATİP. (Legacy: IsgKatipDk)</summary>
    public int OhsKatipMinutes { get; set; }

    /// <summary>Minutes actually consumed in İSG-KATİP. (Legacy: IsgKatipKulDk)</summary>
    public int OhsKatipUsedMinutes { get; set; }

    /// <summary>Whether the employee receives a meal allowance. (Legacy: YemekliMi)</summary>
    public bool IncludesMeal { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
