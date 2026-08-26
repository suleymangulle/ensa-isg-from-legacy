using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// IBYS work environment type (the parent grouping).
/// <para>Legacy equivalent: <c>IBYSWorkEnvironmentTypes_T</c>.</para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class IbysWorkEnvironmentType : AuditedEntity, IActivatable
{
    /// <summary>IBYS type code. (Legacy: <c>TurKodu</c>)</summary>
    public int TypeCode { get; set; }

    /// <summary>Type name. (Legacy: <c>TurAdi</c>)</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Whether the code is still in use. (Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
