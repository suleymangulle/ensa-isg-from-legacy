using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication.Dtos;

namespace Ensa.Application.Contracts.Communication;

/// <summary>
/// Visits and appointments a specialist or physician plans and carries out at a workplace.
/// </summary>
public interface IVisitAppService : IApplicationService
{
    Task<VisitDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<VisitListDto>> GetListAsync(
        GetVisitListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a visit. Defaults the visiting user to the caller when none is given.</summary>
    Task<VisitDto> CreateAsync(CreateVisitDto input, CancellationToken cancellationToken = default);

    Task<VisitDto> UpdateAsync(int id, UpdateVisitDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Visits in a date range, shaped for a calendar UI: id, title, start, end, colour and the
    /// workplace name, with the joins already resolved by the repository.
    /// </summary>
    /// <param name="userId">Restrict to one user's calendar; <c>null</c> means everyone.</param>
    /// <param name="from">Range start, inclusive.</param>
    /// <param name="to">Range end, inclusive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ListResultDto<VisitCalendarDto>> GetCalendarAsync(
        int? userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
