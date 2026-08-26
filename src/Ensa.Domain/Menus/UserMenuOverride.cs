using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Menus;

/// <summary>
/// A per-user menu customisation: an entry is shown IN ADDITION to, or HIDDEN from, the default
/// menu.
/// Legacy: <c>KullaniciMenu_T</c> (its tenant column was <c>OrganizationId</c>).
/// <para>
/// The legacy menu-building query (<c>MenuOperations</c>) applied this table as follows:
/// <c>OperationType == "removed"</c> rows were dropped from the menu, and
/// <c>OperationType == "added"</c> rows were added even when the module or permission filter
/// excluded them.
/// </para>
/// </summary>
public class UserMenuOverride : CreationAuditedTenantEntity
{
    public int UserId { get; set; }

    public int MenuItemId { get; set; }

    /// <summary>Direction of the customisation. (Legacy: IslemTuru string — "added"/"removed")</summary>
    public UserMenuOverrideAction Operation { get; set; }
}
