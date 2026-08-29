using Ensa.Domain.Common;

namespace Ensa.Domain.Communication;

/// <summary>
/// POP3/SMTP account settings the organization uses to send mail.
/// <para>Legacy equivalent: <c>AyarlarEmail_T</c>.</para>
/// </summary>
public class EmailSettings : AuditedTenantEntity, IActivatable
{
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted column. In phase 2 it will be encrypted at column level through the EF Core
    /// <c>EncryptedStringConverter</c> — the same approach as <c>User.MedulaPassword</c>.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>POP3</c>)</summary>
    public string Pop3Server { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>SMTP</c>)</summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>Port</c> string → <c>int</c>)</summary>
    public int Port { get; set; }

    /// <summary>Not present in legacy; whether the SMTP/POP3 connection uses SSL/TLS.</summary>
    public bool UseSsl { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
