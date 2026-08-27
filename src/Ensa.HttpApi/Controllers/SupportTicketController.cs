using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Communication.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Support ticket endpoints — <c>api/support-ticket</c>.
/// <para>
/// The opener of a ticket and the author of each thread message come from the access token, not
/// from the request body.
/// </para>
/// </summary>
public class SupportTicketController(ISupportTicketAppService supportTicketAppService) : EnsaController
{
    /// <summary>Returns a single ticket.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SupportTicketDto> GetAsync(int id, CancellationToken cancellationToken)
        => supportTicketAppService.GetAsync(id, cancellationToken);

    /// <summary>The ticket with its opener, its responder and the full message thread.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<SupportTicketNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SupportTicketNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => supportTicketAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable ticket list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<SupportTicketListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<SupportTicketListDto>> GetListAsync(
        [FromQuery] GetSupportTicketListInput input,
        CancellationToken cancellationToken)
        => supportTicketAppService.GetListAsync(input, cancellationToken);

    /// <summary>Opens a ticket in the caller's name.</summary>
    [HttpPost]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<SupportTicketDto> CreateAsync(
        [FromBody] CreateSupportTicketDto input,
        CancellationToken cancellationToken)
        => supportTicketAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the subject or the assigned responder.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SupportTicketDto> UpdateAsync(
        int id,
        [FromBody] UpdateSupportTicketDto input,
        CancellationToken cancellationToken)
        => supportTicketAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the ticket together with its thread.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => supportTicketAppService.DeleteAsync(id, cancellationToken);

    // --------------------------------------------------------------- Messages

    /// <summary>The message thread of one ticket, oldest first.</summary>
    [HttpGet("{id:int}/messages")]
    [ProducesResponseType<ListResultDto<SupportTicketMessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<SupportTicketMessageDto>> GetMessagesAsync(
        int id,
        CancellationToken cancellationToken)
        => supportTicketAppService.GetMessagesAsync(id, cancellationToken);

    /// <summary>Posts a message into the thread. Refused on a closed ticket.</summary>
    [HttpPost("{id:int}/messages")]
    [ProducesResponseType<SupportTicketMessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SupportTicketMessageDto> AddMessageAsync(
        int id,
        [FromBody] AddSupportTicketMessageDto input,
        CancellationToken cancellationToken)
        => supportTicketAppService.AddMessageAsync(id, input, cancellationToken);

    /// <summary>Closes the ticket.</summary>
    [HttpPost("{id:int}/close")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SupportTicketDto> CloseAsync(int id, CancellationToken cancellationToken)
        => supportTicketAppService.CloseAsync(id, cancellationToken);

    /// <summary>Reopens a closed or cancelled ticket.</summary>
    [HttpPost("{id:int}/reopen")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SupportTicketDto> ReopenAsync(int id, CancellationToken cancellationToken)
        => supportTicketAppService.ReopenAsync(id, cancellationToken);

    /// <summary>Number of the caller's own tickets that are not yet closed.</summary>
    [HttpGet("open-count")]
    [ProducesResponseType<OpenSupportTicketCountDto>(StatusCodes.Status200OK)]
    public Task<OpenSupportTicketCountDto> GetOpenCountAsync(CancellationToken cancellationToken)
        => supportTicketAppService.GetOpenCountAsync(cancellationToken);
}
