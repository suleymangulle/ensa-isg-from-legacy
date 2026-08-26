using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A training category or group, e.g. "General Subjects" or "Technical Trainings".
/// <para>Legacy equivalent: <c>TrainingGroup_T</c>.</para>
/// <para>
/// The legacy table had no tenant column, so this is modelled as a host reference table with no
/// tenant.
/// </para>
/// </summary>
public class TrainingGroup : AuditedEntity
{
    public string TrainingGroupName { get; set; } = string.Empty;

    public string? TrainingGroupCode { get; set; }

    /// <summary>Sort priority in listings. (Legacy: <c>Sira</c>)</summary>
    public int? OrderNo { get; set; }
}
