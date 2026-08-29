using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Communication;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Communication;

/// <summary>
/// The organization's outgoing mail account.
/// <para>
/// <b>The password is never read back.</b> It is stored in an encrypted column and no DTO here
/// carries it outwards — the read model reports only whether one is set. An update that leaves
/// the password empty keeps the stored value, so a settings screen never has to round-trip a
/// secret it was not allowed to see.
/// </para>
/// <para>
/// One account is active per tenant, which is what <c>IMailAppService</c>'s background sender
/// looks for. The entity and its encrypted column existed before this service did, so the
/// account could be modelled but not configured; this closes that gap.
/// </para>
/// </summary>
public class EmailSettingsAppService(
    IServiceProvider serviceProvider,
    IRepository<EmailSettings> emailSettingsRepository)
    : EnsaAppService(serviceProvider), IEmailSettingsAppService
{
    /// <inheritdoc />
    public async Task<EmailSettingsDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Default);

        var settings = await FindCurrentAsync(cancellationToken);

        return settings is null ? null : Map(settings);
    }

    /// <inheritdoc />
    public async Task<EmailSettingsDto> SaveAsync(
        SaveEmailSettingsDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Mail.Update);

        var settings = await FindCurrentAsync(cancellationToken);
        var isNew = settings is null;

        if (isNew && string.IsNullOrWhiteSpace(input.Password))
        {
            // On the first save there is nothing to keep, so an empty password would store an
            // account that can never authenticate.
            throw new EnsaValidationException(
                nameof(SaveEmailSettingsDto.Password),
                "A password is required when the mail account is first configured.");
        }

        settings ??= new EmailSettings();

        settings.Email = input.Email.Trim();
        settings.Pop3Server = input.Pop3Server.Trim();
        settings.SmtpServer = input.SmtpServer.Trim();
        settings.Port = input.Port;
        settings.UseSsl = input.UseSsl;
        settings.IsActive = input.IsActive;

        // An empty password means "leave the stored one alone" — never overwrite a secret with
        // a blank because a form did not send it.
        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            settings.Password = input.Password;
        }

        settings = isNew
            ? await emailSettingsRepository.InsertAsync(settings, autoSave: true, cancellationToken)
            : await emailSettingsRepository.UpdateAsync(settings, autoSave: true, cancellationToken);

        // The password is deliberately absent from this log line.
        Logger.LogInformation(
            "Mail account saved. SettingsId={SettingsId}, SmtpServer={SmtpServer}, Port={Port}, Ssl={Ssl}",
            settings.Id, settings.SmtpServer, settings.Port, settings.UseSsl);

        return Map(settings);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Delete);

        var settings = await FindCurrentAsync(cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(EmailSettings), 0);

        await emailSettingsRepository.DeleteAsync(settings, autoSave: true, cancellationToken);

        Logger.LogInformation("Mail account deleted. SettingsId={SettingsId}", settings.Id);
    }

    /// <summary>
    /// The tenant's account. The global query filter already restricts the set to the current
    /// tenant, so this only picks the active row — the newest one when several exist, which the
    /// unique-per-tenant intent makes unlikely but does not enforce at the database level.
    /// </summary>
    private async Task<EmailSettings?> FindCurrentAsync(CancellationToken cancellationToken)
    {
        var candidates = await emailSettingsRepository.GetListAsync(
            settings => settings.IsActive,
            cancellationToken);

        return candidates.Count == 0
            ? null
            : candidates.OrderByDescending(settings => settings.Id).First();
    }

    private static EmailSettingsDto Map(EmailSettings settings) => new()
    {
        Id = settings.Id,
        TenantId = settings.TenantId,
        Email = settings.Email,
        HasPassword = !string.IsNullOrEmpty(settings.Password),
        Pop3Server = settings.Pop3Server,
        SmtpServer = settings.SmtpServer,
        Port = settings.Port,
        UseSsl = settings.UseSsl,
        IsActive = settings.IsActive,
    };
}
