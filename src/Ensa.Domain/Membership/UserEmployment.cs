using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// How a user is employed by the organization.
/// <para>
/// <b>Why it is separate from the profile.</b> A name is a fact about a person; a salary and a
/// hire date are facts about a contract between that person and the organization. They are read by
/// different screens, edited by different people, and one of them is confidential.
/// </para>
/// <para>
/// <b>Why <c>UserTypeId</c> lives here and <c>StaffRole</c> does not.</b> The <c>User</c> table
/// carried a <c>StaffRole</c> enum while a <c>UserType</c> table already existed carrying the same
/// enum, and nothing joined a user to a type at all — 3,706 legacy rows had a
/// <c>PersonelTuru</c> with nowhere to go. The type is the record; the role is a property of the
/// type, so keeping both on the user was storing the same fact twice and letting them disagree.
/// </para>
/// <para>
/// Distinct from <see cref="StaffCostBaseline"/>, which is a snapshot of a past period and must
/// not change when today's salary does. This is the current arrangement.
/// </para>
/// </summary>
public class UserEmployment : FullAuditedTenantEntity
{
    /// <summary>The account this employment belongs to. FK — no navigation property.</summary>
    public int UserId { get; set; }

    /// <summary>
    /// What kind of user this is: OHS specialist, workplace physician, office staff, customer.
    /// FK — no navigation property. It is also what the permission gates read.
    /// (Legacy: <c>Kullanici_T.PersonelTuru</c>, a free-text code)
    /// </summary>
    public int? UserTypeId { get; set; }

    /// <summary>(Legacy: <c>IseGirisTarihi</c>)</summary>
    public DateTime? HireDate { get; set; }

    /// <summary>(Legacy: <c>IstenCikisTarihi</c>)</summary>
    public DateTime? TerminationDate { get; set; }

    /// <summary>
    /// Gross monthly salary. Money is decimal — the legacy column was a <c>float</c>, which is the
    /// wrong type for money and rounds in ways nobody can predict. (Legacy: <c>BrutMaas</c>)
    /// </summary>
    public decimal? GrossSalary { get; set; }

    /// <summary>(Legacy: <c>PartTime</c>)</summary>
    public bool PartTime { get; set; }

    public override string ToString() => $"[{nameof(UserEmployment)}] UserId = {UserId}";
}
