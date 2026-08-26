using Ensa.Domain.Common;

namespace Ensa.Domain.Plans;

/// <summary>
/// An activity category or group.
/// <para>Legacy equivalent: <c>ActivityGroup_T</c>.</para>
/// </summary>
public class ActivityGroup : AuditedEntity, IActivatable
{
    public string GroupName { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>Aktif</c> bool?)</summary>
    public bool IsActive { get; set; } = true;
}
