using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Tenant-scoped key/value system setting.
/// <para>Legacy equivalent: <c>Parameter_T</c>.</para>
/// </summary>
public class Parameter : AuditedTenantEntity, IActivatable
{
    /// <summary>Parameter code, unique within the tenant. (Legacy: <c>ParameterCode</c>)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name. (Legacy: <c>ParameterName</c>)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Parameter value. (Legacy: <c>ParameterValue</c>)</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
