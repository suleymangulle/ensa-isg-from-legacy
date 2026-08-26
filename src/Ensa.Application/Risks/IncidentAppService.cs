using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Repositories;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Risks;

/// <summary>
/// Work accident / near miss / occupational disease application service.
/// <para>
/// <see cref="IIncidentManager"/> owns the record validation ("the incident date cannot be in
/// the future", work-resumption and notification ordering) and the SSI notification window of
/// three working days (act 5510 art. 13). It is a <b>pure validator/calculator — it performs no
/// persistence</b>, so this service saves the entity itself after calling it.
/// </para>
/// </summary>
public class IncidentAppService(
    IServiceProvider serviceProvider,
    IIncidentRepository incidentRepository,
    IRepository<IncidentPerson> incidentPersonRepository,
    IReadOnlyRepository<Company> companyRepository,
    IReadOnlyRepository<WorkplaceDepartment> departmentRepository,
    IIncidentManager incidentManager)
    : EnsaAppService(serviceProvider), IIncidentAppService
{
    /// <inheritdoc />
    public async Task<IncidentDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Incident.Default);

        var incident = await incidentRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Incident), id);

        return MapIncident(incident);
    }

    /// <inheritdoc />
    public async Task<IncidentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Incident.Default);

        // Department, document, supervisor and all three person lists arrive in one call.
        var navigation = await incidentRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Incident), id);

        return new IncidentNavigationDto
        {
            Incident = MapIncident(navigation.Incident),
            Company = RiskLookupHelper.Lookup(navigation.Company?.Id, navigation.Company?.CompanyName),
            Department = RiskLookupHelper.Lookup(
                navigation.Department?.Id, navigation.Department?.DepartmentName),
            Document = RiskLookupHelper.Lookup(navigation.Document?.Id, navigation.Document?.DocumentName),
            UnitSupervisor = RiskLookupHelper.Lookup(
                navigation.UnitSupervisor?.Id,
                navigation.UnitSupervisor is { } supervisor
                    ? $"{supervisor.Name} {supervisor.LastName}".Trim()
                    : null),
            AffectedPersons = ObjectMapper
                .Map<List<IncidentPerson>, List<IncidentPersonDto>>(navigation.AffectedPersons),
            WitnessPersons = ObjectMapper
                .Map<List<IncidentPerson>, List<IncidentPersonDto>>(navigation.WitnessPersons),
            ResponderPersons = ObjectMapper
                .Map<List<IncidentPerson>, List<IncidentPersonDto>>(navigation.Responders)
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<IncidentListDto>> GetListAsync(
        GetIncidentListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Incident.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "IncidentDate DESC");

        var total = await incidentRepository.GetCountAsync(predicate, cancellationToken);

        var records = await incidentRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Incident>, List<IncidentListDto>>(records);

        // Two batched queries for the page; the notification figures are pure calculations
        // performed in memory by the manager, so they cost no extra round trips.
        var companyNames = await RiskLookupHelper.LoadCompanyNamesAsync(
            companyRepository,
            RiskLookupHelper.DistinctIds(records, i => i.CompanyId),
            cancellationToken);

        var departmentNames = await RiskLookupHelper.LoadDepartmentNamesAsync(
            departmentRepository,
            RiskLookupHelper.DistinctIds(records, i => i.DepartmentId),
            cancellationToken);

        for (var i = 0; i < items.Count; i++)
        {
            items[i].CompanyName = companyNames.GetValueOrDefault(items[i].CompanyId);
            items[i].DepartmentName = departmentNames.GetValueOrDefault(items[i].DepartmentId);
            items[i].LatestSsiNotificationDate = incidentManager.CalculateLatestNotificationDate(records[i]);
            items[i].SsiNotificationOverdue = incidentManager.IsNotificationPeriodOverdue(records[i], Clock.Now);
        }

        return new PagedResultDto<IncidentListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<IncidentDto> CreateAsync(
        CreateIncidentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Incident.Create);

        var incident = ObjectMapper.Map<CreateIncidentDto, Incident>(input);

        // Future-dated incidents, inconsistent work-resumption / notification dates and negative
        // lost work days are rejected here. The manager validates only — it never saves.
        incidentManager.ValidateRecord(incident);

        incident = await incidentRepository.InsertAsync(incident, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Incident created: {IncidentId} — {IncidentType} (Company: {CompanyId})",
            incident.Id, incident.IncidentType, incident.CompanyId);

        return MapIncident(incident);
    }

    /// <inheritdoc />
    public async Task<IncidentDto> UpdateAsync(
        int id,
        UpdateIncidentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Incident.Update);

        var incident = await incidentRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Incident), id);

        ObjectMapper.Map(input, incident);

        incidentManager.ValidateRecord(incident);

        incident = await incidentRepository.UpdateAsync(incident, autoSave: true, cancellationToken);

        return MapIncident(incident);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Incident.Delete);

        var incident = await incidentRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Incident), id);

        var persons = await incidentRepository.GetPersonsAsync(id, personType: null, cancellationToken);

        await incidentPersonRepository.DeleteManyAsync(persons, autoSave: false, cancellationToken);
        await incidentRepository.DeleteAsync(incident, autoSave: true, cancellationToken);

        Logger.LogInformation("Incident deleted: {IncidentId}", id);
    }

    // ----------------------------------------------------------------- Persons

    /// <inheritdoc />
    public async Task<ListResultDto<IncidentPersonDto>> GetPersonsAsync(
        int incidentId,
        IncidentPersonRole? personType = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Incident.Default);

        _ = await incidentRepository.FindAsync(incidentId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Incident), incidentId);

        var persons = await incidentRepository.GetPersonsAsync(incidentId, personType, cancellationToken);

        return new ListResultDto<IncidentPersonDto>(
            ObjectMapper.Map<List<IncidentPerson>, List<IncidentPersonDto>>(persons));
    }

    /// <inheritdoc />
    public async Task<IncidentPersonDto> AddPersonAsync(
        int incidentId,
        CreateIncidentPersonDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Incident.Update);

        _ = await incidentRepository.FindAsync(incidentId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Incident), incidentId);

        var person = ObjectMapper.Map<CreateIncidentPersonDto, IncidentPerson>(input);
        person.IncidentId = incidentId;

        person = await incidentPersonRepository.InsertAsync(person, autoSave: true, cancellationToken);

        return ObjectMapper.Map<IncidentPerson, IncidentPersonDto>(person);
    }

    /// <inheritdoc />
    public async Task RemovePersonAsync(
        int incidentId,
        int personId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Incident.Update);

        var person = await incidentPersonRepository.FindAsync(personId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(IncidentPerson), personId);

        if (person.IncidentId != incidentId)
        {
            throw new EntityNotFoundException(typeof(IncidentPerson), personId);
        }

        await incidentPersonRepository.DeleteAsync(person, autoSave: true, cancellationToken);
    }

    // --------------------------------------------------------------- Analytics

    /// <inheritdoc />
    public async Task<LostWorkDaysSummaryDto> GetTotalLostWorkDaysAsync(
        int companyId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Incident.Default);

        if (from.Date > to.Date)
        {
            throw new BusinessException(
                    "The start date of the period cannot be later than its end date.",
                    "Ensa:Incident:InvalidDateRange")
                .WithData("From", from)
                .WithData("To", to);
        }

        var total = await incidentRepository.GetTotalLostWorkDaysAsync(companyId, from, to, cancellationToken);

        return new LostWorkDaysSummaryDto
        {
            CompanyId = companyId,
            From = from,
            To = to,
            TotalLostWorkDays = total
        };
    }

    // ----------------------------------------------------------------- Helpers

    private IncidentDto MapIncident(Incident incident)
    {
        var dto = ObjectMapper.Map<Incident, IncidentDto>(incident);

        // Every SSI figure comes from the manager; the 3-working-day rule is not duplicated here.
        dto.LatestSsiNotificationDate = incidentManager.CalculateLatestNotificationDate(incident);
        dto.SsiNotificationOverdue = incidentManager.IsNotificationPeriodOverdue(incident, Clock.Now);
        dto.RemainingSsiNotificationWorkDays = incidentManager.RemainingNotificationWorkDays(incident, Clock.Now);

        return dto;
    }

    private static Expression<Func<Incident, bool>>? BuildFilter(GetIncidentListInput input)
    {
        var filter = new RiskFilter<Incident>();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            filter.Add(i =>
                (i.Description != null && i.Description.Contains(search))
                || (i.Expression != null && i.Expression.Contains(search))
                || (i.SupervisorFullName != null && i.SupervisorFullName.Contains(search)));
        }

        filter.AddIf(input.CompanyId is { }, i => i.CompanyId == input.CompanyId!.Value);
        filter.AddIf(input.DepartmentId is { }, i => i.DepartmentId == input.DepartmentId!.Value);
        filter.AddIf(input.IncidentType is { }, i => i.IncidentType == input.IncidentType!.Value);
        filter.AddIf(input.AccidentType is { }, i => i.AccidentType == input.AccidentType!.Value);
        if (input.IncidentFrom is { } from)
        {
            filter.Add(i => i.IncidentDate >= from);
        }

        if (input.IncidentTo is { } to)
        {
            filter.Add(i => i.IncidentDate <= to);
        }


        // Only accidents and occupational diseases carry an SSI notification obligation.
        filter.AddIf(
            input.OnlySsiNotificationPending,
            i => i.SsiNotificationDate == null
                 && (i.IncidentType == IncidentType.WorkAccident
                     || i.IncidentType == IncidentType.OccupationalDisease));

        return filter.Build();
    }
}
