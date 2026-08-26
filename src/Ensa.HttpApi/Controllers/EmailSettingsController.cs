using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Outgoing mail account endpoints — <c>api/email-settings</c>.
/// <para>
/// One account per organization. The password is write-only throughout: it is stored in an
/// encrypted column and no response here carries it, so a settings screen can report that a
/// password is set without ever receiving it.
/// </para>
/// </summary>
public class EmailSettingsController(IEmailSettingsAppService emailSettingsAppService) : EnsaController
{
    /// <summary>
    /// The organization's account, or <c>204 No Content</c> when none is configured.
    /// <para>
    /// A missing account is the normal state of a fresh installation, not an error, so it is
    /// reported as "nothing here" rather than as a 404.
    /// </para>
    /// </summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Mail.Default)]
    [ProducesResponseType<EmailSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<EmailSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await emailSettingsAppService.GetAsync(cancellationToken);
        return settings is null ? NoContent() : Ok(settings);
    }

    /// <summary>
    /// Creates the account or updates the existing one. An empty password on an update keeps the
    /// stored value.
    /// </summary>
    [HttpPut]
    [Authorize(EnsaPermissions.Mail.Update)]
    [ProducesResponseType<EmailSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EmailSettingsDto> SaveAsync(
        [FromBody] SaveEmailSettingsDto input,
        CancellationToken cancellationToken)
        => emailSettingsAppService.SaveAsync(input, cancellationToken);

    /// <summary>Removes the configured account.</summary>
    [HttpDelete]
    [Authorize(EnsaPermissions.Mail.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(CancellationToken cancellationToken)
    {
        await emailSettingsAppService.DeleteAsync(cancellationToken);
        return NoContent();
    }
}
