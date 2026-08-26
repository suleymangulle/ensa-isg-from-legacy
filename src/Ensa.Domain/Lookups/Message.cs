using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Dictionary of in-application notification texts (e.g. validation/error/success message
/// templates).
/// <para>Legacy equivalent: <c>Message_T</c>.</para>
/// <para>Host-level (tenant-less) reference table.</para>
/// </summary>
public class Message : AuditedEntity
{
    /// <summary>Message type. FK — no navigation property.</summary>
    public int MessageTemplateTypeId { get; set; }

    /// <summary>Unique message code. (Legacy: <c>MessageCode</c>)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Message body. (Legacy: <c>Message</c>)</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>CSS class to apply in the user interface.</summary>
    public string? CssClass { get; set; }
}
