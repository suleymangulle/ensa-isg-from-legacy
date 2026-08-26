using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// Assignment paperwork for a specialist/physician assigned to a company (contract, İSG-KATİP
/// printout, and the like).
/// <para>Legacy equivalent: <c>AssignedSpecialistDocument_T</c>.</para>
/// </summary>
public class AssignedSpecialistDocument : FullAuditedTenantEntity, IActivatable
{
    public int AssignedSpecialistId { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int DocumentId { get; set; }

    /// <summary>Date the document was issued.</summary>
    public DateTime DocumentDate { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
