using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// System-wide configuration value (e.g. an SMTP setting or an API key).
/// <para>Legacy equivalent: <c>StandardDegiskenler_T</c>.</para>
/// <para>
/// NOTE: in the legacy schema the primary key was the <c>string DegiskenName</c> column. It has
/// been normalised to <see cref="Entity.Id"/> (int, auto-increment); <see cref="SettingName"/>
/// gets a UNIQUE index via the Fluent API in phase 2.
/// </para>
/// <para>Host-level (tenant-less) system table.</para>
/// </summary>
public class SystemSetting : Entity
{
    /// <summary>Setting name — gets a unique index in phase 2. (Legacy: PK <c>DegiskenName</c>)</summary>
    public string SettingName { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    /// <summary>Data type of the value (free text — e.g. "string", "int", "bool").</summary>
    public string SettingType { get; set; } = string.Empty;

    /// <summary>Whether the value must be stored encrypted/secret (e.g. an API key).</summary>
    public bool Encrypted { get; set; }

    /// <summary>Whether the value can be changed from the user interface.</summary>
    public bool IsEditable { get; set; } = true;
}
