using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Category of in-application notification messages (e.g. success, error, warning).
/// <para>Legacy equivalent: <c>MessageType_T</c>.</para>
/// <para>Host-level (tenant-less) reference table.</para>
/// </summary>
public class MessageTemplateType : AuditedEntity
{
    /// <summary>Unique message type code. (Legacy: <c>MessageTypeCode</c>)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name. (Legacy: <c>MessageType</c>)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>CSS class to apply in the user interface (e.g. "alert-success").</summary>
    public string? CssClass { get; set; }
}
