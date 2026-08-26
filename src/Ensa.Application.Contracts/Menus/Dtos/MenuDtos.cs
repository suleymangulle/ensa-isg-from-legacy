using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;

namespace Ensa.Application.Contracts.Menus.Dtos;

/// <summary>Menu list row.</summary>
public class MenuListDto : EntityDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Layout type code (left menu, top menu, quick access ...).</summary>
    public string? MenuTypeCode { get; set; }

    /// <summary>Staff-role code the menu is served to. <c>null</c> means every staff role.</summary>
    public string? UserTypeCode { get; set; }

    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Menu detail view - the root of a menu tree.</summary>
public class MenuDto : AuditedEntityDto
{
    public string Name { get; set; } = string.Empty;
    public string? MenuTypeCode { get; set; }
    public string? UserTypeCode { get; set; }

    /// <summary>Organization type the menu applies to. <c>null</c> means every organization type.</summary>
    public int? OrganizationTypeId { get; set; }

    /// <summary>Subscription plan the menu applies to. <c>null</c> means every plan.</summary>
    public int? SubscriptionPlanId { get; set; }

    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// A reusable catalogue entry - the title, icon and URL of a page or action. The same item
/// can appear in several menus through different <c>MenuNode</c> placements.
/// </summary>
public class MenuItemDto : AuditedEntityDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }

    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public string? LongDescription { get; set; }

    public string? Url { get; set; }
    public string? QueryStringKeys { get; set; }
    public string? ExtraAttributes { get; set; }

    public string? IconCssClass { get; set; }
    public string? CssClass { get; set; }
    public string? CssClass2 { get; set; }
    public string? CssStyle { get; set; }

    /// <summary>
    /// Module that makes this item visible. When set, the item only shows if the company has
    /// that module enabled. <c>null</c> means no module gate.
    /// </summary>
    public int? ModuleId { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Menu list filter.</summary>
public class GetMenuListInput : PagedAndSortedFilterDto
{
    public string? MenuTypeCode { get; set; }
    public string? UserTypeCode { get; set; }
    public int? OrganizationTypeId { get; set; }
    public int? SubscriptionPlanId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>Request payload for the menu of the signed-in user.</summary>
public class GetUserMenuInput
{
    /// <summary>Layout type code of the requested menu, e.g. the left navigation bar.</summary>
    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string MenuTypeCode { get; set; } = string.Empty;
}
