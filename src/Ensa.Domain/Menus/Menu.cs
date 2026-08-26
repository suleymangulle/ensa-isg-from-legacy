using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// A menu definition — the root of the menu tree served to one combination of user type,
/// organization type and subscription plan.
/// <para>
/// <b>MERGE:</b> the legacy <c>Menu_T</c> and <c>NewMenu_T</c> tables are MERGED into this single
/// entity. <c>NewMenu_T</c> was an unfinished new version of the menu infrastructure; its columns
/// (<c>MenuId</c>, <c>MenuName</c>, <c>AddDate</c>, <c>UpdateDate</c>, <c>Active</c>) are a SUBSET
/// of <c>Menu_T</c>, and although it was declared as a <c>DbSet</c> on <c>CRMContext</c> no
/// business code ever used it. Keeping it separate was therefore unnecessary: a <c>NewMenu_T</c>
/// row corresponds to a <c>Menu</c> row whose type, organization and plan fields are <c>null</c>.
/// </para>
/// <para>
/// Menu entries attach in two different shapes, and BOTH point back at this root:
/// <list type="bullet">
///   <item><see cref="MenuNode"/> → the classic shape, which places an entry from the shared
///         <see cref="MenuItem"/> catalogue (legacy <c>MenuDetail_T</c>).</item>
///   <item><see cref="MenuElement"/> → the free-form shape, which carries its own text, icon and
///         URL (legacy <c>MenuElement_T</c>).</item>
/// </list>
/// </para>
/// <para>A host reference table; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class Menu : AuditedEntity, IActivatable, IHasSortOrder
{
    /// <summary>(Legacy: MenuAdi)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Layout type code of the menu; it matches <see cref="MenuType"/>.<c>Code</c>
    /// (side menu, top menu, quick access and so on). (Legacy: MenuTypeCode)
    /// </summary>
    public string? MenuTypeCode { get; set; }

    /// <summary>
    /// Code of the user type this menu is served to; it matches <c>UserType.Code</c>.
    /// <c>null</c> means every user type. (Legacy: KullaniciType)
    /// </summary>
    public string? UserTypeCode { get; set; }

    /// <summary>
    /// FK to the organization type this menu applies to. <c>null</c> means every organization type.
    /// (Legacy: the <c>KurumTuru</c> string code, normalized into an FK.)
    /// </summary>
    public int? OrganizationTypeId { get; set; }

    /// <summary>
    /// FK to the subscription plan this menu applies to. <c>null</c> means every plan.
    /// (Legacy: the <c>PaketTuru</c> string code, normalized into an FK.)
    /// </summary>
    public int? SubscriptionPlanId { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
