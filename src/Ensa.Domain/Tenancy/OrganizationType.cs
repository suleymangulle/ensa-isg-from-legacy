using Ensa.Domain.Common;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// Reference table of organization types: OHS service provider (OSGB), enterprise, ministry and so
/// on.
/// Legacy: <c>KurumTuru_T</c>.
/// <para>Host table — shared by every tenant; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class OrganizationType : AuditedEntity, IActivatable, IHasSortOrder
{
    /// <summary>Unique code. (Legacy: KurumTuruKodu — Menu_T and Firma_T matched on it.)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name. (Legacy: KurumTuruAdi)</summary>
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
