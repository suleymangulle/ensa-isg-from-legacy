using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// IBYS work arrangement code. These codes are used in the
/// <c>Health.MedicalExaminationForm.IbysWorkArrangementCodes</c> column of the examination form.
/// <para>Legacy equivalent: <c>IBYSWorkSekilleri_T</c>.</para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class IbysWorkArrangement : AuditedEntity, IActivatable
{
    /// <summary>IBYS work arrangement code. (Legacy: <c>Kod</c>)</summary>
    public int Code { get; set; }

    /// <summary>Name of the work arrangement. (Legacy: <c>Ad</c>)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Parent grouping/type code. (Legacy: <c>Tur</c>)</summary>
    public int Type { get; set; }

    /// <summary>Free-text description. (Legacy: <c>Aciklama</c>)</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the code is still in use.
    /// <para>
    /// The legacy table had NO such column (it carried no active flag); it was added for
    /// consistency with the other IBYS reference tables and is seeded as <c>true</c>.
    /// </para>
    /// </summary>
    public bool IsActive { get; set; } = true;
}
