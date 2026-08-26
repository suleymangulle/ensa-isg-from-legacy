namespace Ensa.Domain.Shared.Localization;

/// <summary>
/// Marker type for the application's localization resources.
/// <para>
/// Consumers inject <c>IStringLocalizer&lt;EnsaResource&gt;</c>. The satellite
/// resource files live next to this type:
/// </para>
/// <list type="bullet">
/// <item><c>EnsaResource.resx</c> — invariant / English (fallback)</item>
/// <item><c>EnsaResource.tr.resx</c> — Turkish</item>
/// </list>
/// <para>
/// Keys mirror the <see cref="Exceptions.BusinessException.Code"/> values so a
/// thrown business error can be translated without a lookup table, e.g.
/// <c>Ensa:Company:SsiNumberAlreadyRegistered</c>.
/// </para>
/// </summary>
public sealed class EnsaResource
{
    private EnsaResource()
    {
    }
}
