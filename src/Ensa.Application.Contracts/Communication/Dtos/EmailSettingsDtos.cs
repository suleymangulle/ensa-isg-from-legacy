using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;

namespace Ensa.Application.Contracts.Communication.Dtos;

/// <summary>
/// The organization's outgoing mail account.
/// <para>
/// <b>The password is never returned.</b> It is stored in an encrypted column and there is no
/// read path for it — a settings screen shows whether one is set, never its value.
/// </para>
/// </summary>
public class EmailSettingsDto : EntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Whether a password is stored. The value itself is never exposed.</summary>
    public bool HasPassword { get; set; }

    public string Pop3Server { get; set; } = string.Empty;

    public string SmtpServer { get; set; } = string.Empty;

    public int Port { get; set; }

    /// <summary>Whether the connection uses SSL/TLS.</summary>
    public bool SslUse { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// Create or update input for the outgoing mail account.
/// <para>
/// <see cref="Password"/> is write-only. Leaving it empty on an update keeps the stored password,
/// so a screen never has to round-trip a secret it was not allowed to read in the first place.
/// </para>
/// </summary>
public class SaveEmailSettingsDto
{
    [Required(ErrorMessage = "The e-mail address is required.")]
    [EmailAddress(ErrorMessage = "A valid e-mail address is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// New password. Required when creating the account; optional afterwards — an empty value
    /// means "keep the current one".
    /// </summary>
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "The POP3 server is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string Pop3Server { get; set; } = string.Empty;

    [Required(ErrorMessage = "The SMTP server is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string SmtpServer { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "The port must be between 1 and 65535.")]
    public int Port { get; set; } = 587;

    public bool SslUse { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
