using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication.Dtos;

namespace Ensa.Application.Contracts.Communication;

/// <summary>
/// The organization's outgoing mail account — the SMTP/POP3 credentials the mail sender uses.
/// <para>
/// One account is active per tenant. <c>IMailAppService</c> queues messages; without this the
/// queue has nothing to send them with, which is why the entity existed with no way to fill it.
/// </para>
/// </summary>
public interface IEmailSettingsAppService : IApplicationService
{
    /// <summary>
    /// The active account of the current tenant, or <c>null</c> when none has been configured.
    /// A miss is an ordinary outcome here, not an error — it is what a fresh installation looks
    /// like.
    /// </summary>
    Task<EmailSettingsDto?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the account or updates the existing one. Leaving the password empty on an update
    /// keeps the stored value.
    /// </summary>
    Task<EmailSettingsDto> SaveAsync(SaveEmailSettingsDto input, CancellationToken cancellationToken = default);

    /// <summary>Removes the configured account. Queued mail then has nothing to send it with.</summary>
    Task DeleteAsync(CancellationToken cancellationToken = default);
}
