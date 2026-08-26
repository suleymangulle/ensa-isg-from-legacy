using Ensa.Domain.Repositories;
using Ensa.Domain.Risks.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>Queries specific to incident and accident records.</summary>
public interface IIncidentRepository : IRepository<Incident>
{
    /// <summary>Loads the incident with its department, document and person lists (affected, witnesses, responders).</summary>
    Task<IncidentNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists a company's incidents in the given date range, optionally filtered by type.
    /// </summary>
    Task<List<Incident>> GetListByCompanyAsync(
        int companyId,
        DateTime? start = null,
        DateTime? end = null,
        IncidentType? incidentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists work accidents not yet reported to the SSI, to track the three-working-day reporting
    /// obligation.
    /// </summary>
    Task<List<Incident>> GetPendingSsiNotificationsAsync(
        int? companyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total lost working days in the period, an input to the accident frequency and
    /// severity rates.
    /// </summary>
    Task<int> GetTotalLostWorkDaysAsync(
        int companyId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the people linked to an incident: affected, witnesses and responders.</summary>
    Task<List<IncidentPerson>> GetPersonsAsync(
        int incidentId,
        IncidentPersonRole? personType = null,
        CancellationToken cancellationToken = default);
}
