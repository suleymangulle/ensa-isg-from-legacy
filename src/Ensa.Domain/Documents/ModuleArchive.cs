using Ensa.Domain.Common;

namespace Ensa.Domain.Documents;

/// <summary>
/// The header of an office-scoped bulk module archive, e.g. "January 2026 payroll package".
/// <para>Legacy equivalent: <c>ModuleArchive_T</c>.</para>
/// <para>
/// The legacy <c>ModuleAddDate</c> column is covered by the base class <c>CreationTime</c>; no
/// separate field was added.
/// </para>
/// </summary>
public class ModuleArchive : AuditedTenantEntity
{
    /// <summary>Module name.</summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>Module code.</summary>
    public string ModuleCode { get; set; } = string.Empty;
}
