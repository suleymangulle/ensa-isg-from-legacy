using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// View configuration that defines which field appears on the sales rep screens, in which context,
/// under which caption and in which order.
/// Legacy: <c>TemGosterAlan</c>.
/// <para>Host configuration; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class SalesRepScreenField : AuditedEntity, IHasSortOrder
{
    /// <summary>Technical name of the source model field. (Legacy: AlanAdi)</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Caption shown on screen. (Legacy: GAdi)</summary>
    public string DisplayedName { get; set; } = string.Empty;

    /// <summary>Whether the field is shown. (Legacy: Goster)</summary>
    public bool Show { get; set; } = true;

    /// <summary>Which sales rep screen the field belongs to. (Legacy: TemTuru int)</summary>
    public SalesRepScreenType ScreenType { get; set; } = SalesRepScreenType.Unspecified;

    /// <summary>Display order. (Legacy: GosSirasi int?)</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether the field is shown in the list or table view. (Legacy: TablodaGoster bool?)</summary>
    public bool InTableShow { get; set; } = true;

    /// <summary>Whether the field is shown in the detail popup. (Legacy: PopuptaGoster bool?)</summary>
    public bool InPopupShow { get; set; } = true;
}
