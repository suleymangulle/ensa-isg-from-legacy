using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;
using Ensa.Domain.Risks;
using Ensa.Domain.Risks.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Risks;

/// <summary>
/// EF Core implementation of <see cref="IIncidentRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class IncidentRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Incident>(context, dataFilter), IIncidentRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the affected/witness/responder lists are not fetched with three separate
    /// queries but with a <b>single</b> <c>IncidentPerson</c> query for the incident, then split by type
    /// in memory. The total query count is at most 5.
    /// </remarks>
    public async Task<IncidentNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (incident is null)
        {
            return null;
        }

        var navigation = new IncidentNavigation { Incident = incident };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == incident.CompanyId, cancellationToken);

        navigation.Department = await Context.Set<WorkplaceDepartment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == incident.DepartmentId, cancellationToken);

        if (incident.DocumentId is { } documentId)
        {
            navigation.Document = await Context.Set<Document>()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        }

        if (incident.UnitSupervisorId is { } supervisorId)
        {
            navigation.UnitSupervisor = await Context.Set<CompanyEmployee>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == supervisorId, cancellationToken);
        }

        var persons = await Context.Set<IncidentPerson>()
            .AsNoTracking()
            .Where(k => k.IncidentId == id)
            .OrderBy(k => k.Id)
            .ToListAsync(cancellationToken);

        navigation.AffectedPersons = persons.FindAll(k => k.PersonType == IncidentPersonRole.Affected);
        navigation.WitnessPersons = persons.FindAll(k => k.PersonType == IncidentPersonRole.Witness);
        navigation.Responders = persons.FindAll(k => k.PersonType == IncidentPersonRole.Responder);

        return navigation;
    }

    /// <inheritdoc />
    public Task<List<Incident>> GetListByCompanyAsync(
        int companyId,
        DateTime? start = null,
        DateTime? end = null,
        IncidentType? incidentType = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable().Where(o => o.CompanyId == companyId);

        // No function is applied to the date column; the range is written as a half-open interval
        // [start, end) so that the (CompanyId, IncidentDate) index can be used.
        if (start is { } startValue)
        {
            var lowerBound = startValue.Date;
            query = query.Where(o => o.IncidentDate >= lowerBound);
        }

        if (end is { } endValue)
        {
            var upperBound = endValue.Date.AddDays(1);
            query = query.Where(o => o.IncidentDate < upperBound);
        }

        if (incidentType is { } type)
        {
            query = query.Where(o => o.IncidentType == type);
        }

        return query
            .OrderByDescending(o => o.IncidentDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<Incident>> GetPendingSsiNotificationsAsync(
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable()
            .Where(o => o.IncidentType == IncidentType.WorkAccident && o.SsiNotificationDate == null);

        if (companyId is { } value)
        {
            query = query.Where(o => o.CompanyId == value);
        }

        // The oldest incident is the most urgent notification; the three-working-day obligation is tracked accordingly.
        return query
            .OrderBy(o => o.IncidentDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>The total is computed <b>in the database</b>; the rows are not loaded into memory.</remarks>
    public async Task<int> GetTotalLostWorkDaysAsync(
        int companyId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        var lowerBound = start.Date;
        var upperBound = end.Date.AddDays(1);

        return await GetReadOnlyQueryable()
            .Where(o => o.CompanyId == companyId
                        && o.IncidentDate >= lowerBound
                        && o.IncidentDate < upperBound)
            .SumAsync(o => o.LostWorkDays ?? 0, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<IncidentPerson>> GetPersonsAsync(
        int incidentId,
        IncidentPersonRole? personType = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<IncidentPerson>()
            .AsNoTracking()
            .Where(k => k.IncidentId == incidentId);

        if (personType is { } type)
        {
            query = query.Where(k => k.PersonType == type);
        }

        return query
            .OrderBy(k => k.PersonType)
            .ThenBy(k => k.Id)
            .ToListAsync(cancellationToken);
    }
}
