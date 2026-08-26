using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Lookups;

/// <summary>
/// System/application log record (error, information and warning traces).
/// <para>Legacy equivalent: <c>Log_T</c>.</para>
/// <para>
/// Being an append-only record, it carries no modification/deletion audit —
/// <see cref="CreationAuditedTenantEntity"/>. The legacy <c>Date</c> column is covered by
/// <see cref="Common.IHasCreationTime.CreationTime"/> on the base class; no separate column
/// was introduced.
/// </para>
/// </summary>
public class Log : CreationAuditedTenantEntity
{
    /// <summary>Source table/grid row reference (optional, legacy debugging aid). (Legacy: <c>Row</c>)</summary>
    public int? LineNo { get; set; }

    /// <summary>Page/screen the entry originated from. (Legacy: <c>PageName</c>)</summary>
    public string PageName { get; set; } = string.Empty;

    /// <summary>Method the entry originated from. (Legacy: <c>MethodName</c>)</summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>Log message. (Legacy: <c>Message</c>)</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>User that triggered the log entry, when known.</summary>
    public int? UserId { get; set; }

    /// <summary>Free-text parameter dump (e.g. JSON). (Legacy: <c>Parameters</c>)</summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// Log severity. (Legacy: <c>LogType</c> bool? — the <c>null</c>/<c>false</c>/<c>true</c>
    /// triple was normalised into the <see cref="LogLevel"/> enum.)
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
}
