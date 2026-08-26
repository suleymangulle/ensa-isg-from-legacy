using Ensa.Domain.Common;

namespace Ensa.Domain.Risks;

/// <summary>
/// Organization-specific lookup list of equipment document types.
/// (Legacy: <c>CihazEvrakListesi_T</c>)
/// <para>
/// The legacy table had an <c>OrganizationId</c> column, so the list is organization-specific: it
/// implements <c>IMultiTenant</c> and is subject to the tenant filter. The legacy PK was named
/// <c>EquipmentDocumentId</c> — the same name as on <c>EquipmentDocument_T</c> but with a different
/// meaning; it is simply <c>Id</c> here.
/// </para>
/// </summary>
public class EquipmentDocumentType : FullAuditedTenantEntity, IActivatable, IHasSortOrder
{
    /// <summary>Name of the document type. (Legacy: <c>EvrakAdi</c>)</summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>Listing order. (Not present in legacy; added to manage the lookup.)</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether the record is active. (Not present in legacy; added so entries can be deactivated.)</summary>
    public bool IsActive { get; set; } = true;
}
