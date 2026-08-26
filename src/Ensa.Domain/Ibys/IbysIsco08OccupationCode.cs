using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// ISCO-08 occupation code (an employee's occupation is picked from this list on an IBYS
/// submission). The <c>Health.MedicalExaminationForm.IbysOccupationCode</c> column of the
/// examination form carries the <see cref="Code"/> value of this table.
/// <para>Legacy equivalent: <c>IBYSIsco08OccupationCodes_T</c>.</para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class IbysIsco08OccupationCode : AuditedEntity, IActivatable
{
    /// <summary>ISCO-08 code (text — leading zeros are significant). (Legacy: <c>Kod</c>)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Occupation name. (Legacy: <c>Ad</c>)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the code is still in use.
    /// (The legacy table had no such column; it was added for consistency and seeded as
    /// <c>true</c>.)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
