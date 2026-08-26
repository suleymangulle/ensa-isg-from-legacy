using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Communication.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Outbound mail queue endpoints — <c>api/mail</c>.
/// <para>
/// There is no "send" endpoint by design. This API manages the queue; delivery belongs to a
/// background worker that polls <c>pending</c> and reports back through <c>mark-sent</c> and
/// <c>mark-failed</c>. See <see cref="IMailAppService"/> for the full rationale and for the note
/// on that worker not existing yet.
/// </para>
/// </summary>
public class MailController(IMailAppService mailAppService) : EnsaController
{
    /// <summary>Returns a single mail.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Mail.Default)]
    [ProducesResponseType<MailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MailDto> GetAsync(int id, CancellationToken cancellationToken)
        => mailAppService.GetAsync(id, cancellationToken);

    /// <summary>The mail together with its attachments, resolved to file names.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Mail.Default)]
    [ProducesResponseType<MailNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MailNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => mailAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable mail list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Mail.Default)]
    [ProducesResponseType<PagedResultDto<MailListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<MailListDto>> GetListAsync(
        [FromQuery] GetMailListInput input,
        CancellationToken cancellationToken)
        => mailAppService.GetListAsync(input, cancellationToken);

    /// <summary>Creates a mail as a draft.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Mail.Create)]
    [ProducesResponseType<MailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<MailDto> CreateAsync(
        [FromBody] CreateMailDto input,
        CancellationToken cancellationToken)
        => mailAppService.CreateAsync(input, cancellationToken);

    /// <summary>Edits a draft or failed mail. Refused once the mail has been sent.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Mail.Update)]
    [ProducesResponseType<MailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MailDto> UpdateAsync(
        int id,
        [FromBody] UpdateMailDto input,
        CancellationToken cancellationToken)
        => mailAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the mail together with its attachment rows.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Mail.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => mailAppService.DeleteAsync(id, cancellationToken);

    // ------------------------------------------------------------- Lifecycle

    /// <summary>Hands a draft or failed mail to the sending queue.</summary>
    [HttpPost("{id:int}/queue")]
    [Authorize(EnsaPermissions.Mail.Update)]
    [ProducesResponseType<MailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MailDto> QueueAsync(int id, CancellationToken cancellationToken)
        => mailAppService.QueueAsync(id, cancellationToken);

    /// <summary>Queued mails waiting to be sent — the background worker's inbox.</summary>
    [HttpGet("pending")]
    [Authorize(EnsaPermissions.Mail.Default)]
    [ProducesResponseType<ListResultDto<MailDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<MailDto>> GetPendingAsync(
        [FromQuery] int maxResultCount,
        CancellationToken cancellationToken)
        => mailAppService.GetPendingAsync(maxResultCount, cancellationToken);

    /// <summary>Records a successful delivery.</summary>
    [HttpPost("{id:int}/mark-sent")]
    [Authorize(EnsaPermissions.Mail.Update)]
    [ProducesResponseType<MailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MailDto> MarkSentAsync(int id, CancellationToken cancellationToken)
        => mailAppService.MarkSentAsync(id, cancellationToken);

    /// <summary>Records a failed delivery attempt.</summary>
    [HttpPost("{id:int}/mark-failed")]
    [Authorize(EnsaPermissions.Mail.Update)]
    [ProducesResponseType<MailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MailDto> MarkFailedAsync(
        int id,
        [FromBody] MarkMailFailedDto input,
        CancellationToken cancellationToken)
        => mailAppService.MarkFailedAsync(id, input.Error, cancellationToken);

    // ----------------------------------------------------------- Attachments

    /// <summary>Attachments of one mail.</summary>
    [HttpGet("{id:int}/attachments")]
    [Authorize(EnsaPermissions.Mail.Default)]
    [ProducesResponseType<ListResultDto<MailAttachmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<MailAttachmentDto>> GetAttachmentsAsync(int id, CancellationToken cancellationToken)
        => mailAppService.GetAttachmentsAsync(id, cancellationToken);

    /// <summary>Attaches a stored document.</summary>
    [HttpPost("{id:int}/attachments")]
    [Authorize(EnsaPermissions.Mail.Update)]
    [ProducesResponseType<MailAttachmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MailAttachmentDto> AddAttachmentAsync(
        int id,
        [FromBody] AddMailAttachmentDto input,
        CancellationToken cancellationToken)
        => mailAppService.AddAttachmentAsync(id, input, cancellationToken);

    /// <summary>Detaches a file.</summary>
    [HttpDelete("{id:int}/attachments/{attachmentId:int}")]
    [Authorize(EnsaPermissions.Mail.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveAttachmentAsync(int id, int attachmentId, CancellationToken cancellationToken)
        => mailAppService.RemoveAttachmentAsync(id, attachmentId, cancellationToken);
}
