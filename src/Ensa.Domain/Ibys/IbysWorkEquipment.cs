using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// IBYS work equipment code. These codes are used in the
/// <c>Health.MedicalExaminationForm.IbysWorkEquipmentCodes</c> column of the examination form.
/// <para>Legacy equivalent: <c>IBYSWorkEquipment_T</c>.</para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class IbysWorkEquipment : AuditedEntity, IActivatable
{
    /// <summary>IBYS equipment code. (Legacy: <c>Kod</c>)</summary>
    public int Code { get; set; }

    /// <summary>Equipment name. (Legacy: <c>Ad</c>)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Top-level category this record belongs to (<see cref="IbysEquipmentTopCategory"/> FK).
    /// (Legacy: <c>UstKategoriId</c>) There is NO navigation property.
    /// </summary>
    public int ParentCategoryId { get; set; }

    /// <summary>
    /// Whether the code is still in use.
    /// (The legacy table had no such column; it was added for consistency and seeded as
    /// <c>true</c>.)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
