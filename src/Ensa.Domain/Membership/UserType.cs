using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Membership;

/// <summary>
/// User type reference table (OHS specialist, workplace physician, office staff, ...).
/// Legacy: <c>UserType_T</c>.
/// <para>
/// The <see cref="Code"/> value used to be the string form of the <see cref="StaffRole"/> enum
/// and was matched against <c>User_T.StaffRole</c>. In the new model a user's type is held as
/// an enum on <c>User.StaffRole</c>; this table still carries presentation metadata such as the
/// display name and the icon, plus the permission links — which is why it is tied back to the
/// enum through the <see cref="StaffRole"/> property.
/// </para>
/// <para>This is a host reference table and does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class UserType : AuditedEntity, IActivatable, IHasSortOrder
{
    /// <summary>Unique code. (Legacy: KullaniciTypeCode)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name. (Legacy: KullaniciTypeAdi)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The enum value this type maps to — matches <c>User.StaffRole</c>.</summary>
    public StaffRole StaffRole { get; set; } = StaffRole.Unspecified;

    /// <summary>CSS class of the menu/list icon. (Legacy: Icon)</summary>
    public string? IconCssClass { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
