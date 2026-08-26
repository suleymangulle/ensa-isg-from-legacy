using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks;

/// <summary>
/// Work accident / near miss / occupational disease application service.
/// <para>
/// Date validation and the SSI notification window (act 5510 art. 13, 3 working days)
/// belong to <c>IIncidentManager</c>; this service calls it instead of re-implementing them.
/// </para>
/// </summary>
public interface IIncidentAppService : IApplicationService
{
    Task<IncidentDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Detail projection: incident, department, document and the person lists.</summary>
    Task<IncidentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<IncidentListDto>> GetListAsync(
        GetIncidentListInput input,
        CancellationToken cancellationToken = default);

    Task<IncidentDto> CreateAsync(CreateIncidentDto input, CancellationToken cancellationToken = default);

    Task<IncidentDto> UpdateAsync(int id, UpdateIncidentDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ----------------------------------------------------------------- Persons

    Task<ListResultDto<IncidentPersonDto>> GetPersonsAsync(
        int incidentId,
        IncidentPersonRole? personType = null,
        CancellationToken cancellationToken = default);

    Task<IncidentPersonDto> AddPersonAsync(
        int incidentId,
        CreateIncidentPersonDto input,
        CancellationToken cancellationToken = default);

    Task RemovePersonAsync(
        int incidentId,
        int personId,
        CancellationToken cancellationToken = default);

    // --------------------------------------------------------------- Analytics

    /// <summary>Total lost work days in a period — input of the accident frequency / severity rate.</summary>
    Task<LostWorkDaysSummaryDto> GetTotalLostWorkDaysAsync(
        int companyId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
