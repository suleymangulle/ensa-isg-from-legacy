using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Documents;
using Ensa.Domain.Repositories;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Risks;

/// <summary>
/// Emergency action plan application service.
/// <para>
/// The nine legacy free-text columns are normalized into <c>EmergencyPlanSection</c> rows keyed
/// by <see cref="EmergencyPlanSectionType"/>, so section content is upserted one type at a time.
/// </para>
/// <para>
/// The combined read is projected by <c>IEmergencyActionPlanRepository.GetWithNavigationAsync</c>,
/// which keeps the query count fixed; this service only maps the result onto DTOs.
/// </para>
/// </summary>
public class EmergencyActionPlanAppService(
    IServiceProvider serviceProvider,
    IEmergencyActionPlanRepository planRepository,
    IRepository<EmergencyPlanSection> sectionRepository,
    IRepository<EmergencyTeamMember> teamMemberRepository,
    IReadOnlyRepository<Company> companyRepository,
    IRiskAssessmentManager riskAssessmentManager)
    : EnsaAppService(serviceProvider), IEmergencyActionPlanAppService
{
    /// <inheritdoc />
    public async Task<EmergencyActionPlanDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Default);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(EmergencyActionPlan), id);

        return MapPlan(plan);
    }

    /// <inheritdoc />
    public async Task<EmergencyActionPlanNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Default);

        var navigation = await planRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(EmergencyActionPlan), id);

        return new EmergencyActionPlanNavigationDto
        {
            Plan = MapPlan(navigation.Plan),
            Company = navigation.Company is null
                ? null
                : new LookupDto { Id = navigation.Company.Id, DisplayName = navigation.Company.CompanyName },
            EvacuationPlanDocument = DocumentLookup(navigation.EvacuationPlanDocument),
            Document = DocumentLookup(navigation.Document),
            Sections =
            [
                .. ObjectMapper.Map<List<EmergencyPlanSection>, List<EmergencyPlanSectionDto>>(navigation.Sections)
            ],
            TeamMembers =
            [
                .. navigation.TeamMembers.Select(member => new EmergencyTeamMemberNavigationDto
                {
                    TeamMember = ObjectMapper.Map<EmergencyTeamMember, EmergencyTeamMemberDto>(member.TeamMember),
                    Employee = member.Employee is null
                        ? null
                        : new LookupDto
                        {
                            Id = member.Employee.Id,
                            DisplayName = $"{member.Employee.Name} {member.Employee.LastName}".Trim()
                        }
                })
            ]
        };
    }

    /// <summary>Reduces a document to the id/name pair the navigation DTO exposes.</summary>
    private static LookupDto? DocumentLookup(Document? document)
        => document is null
            ? null
            : new LookupDto { Id = document.Id, DisplayName = document.DocumentName };

    /// <inheritdoc />
    public async Task<PagedResultDto<EmergencyActionPlanListDto>> GetListAsync(
        GetEmergencyActionPlanListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Default);

        var reference = Clock.Now.Date;
        var predicate = BuildFilter(input, reference);
        var sorting = NormalizeSorting(input.Sorting, "PreparedDate DESC");

        var total = await planRepository.GetCountAsync(predicate, cancellationToken);

        var records = await planRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<EmergencyActionPlan>, List<EmergencyActionPlanListDto>>(records);

        var companyNames = await RiskLookupHelper.LoadCompanyNamesAsync(
            companyRepository,
            RiskLookupHelper.DistinctIds(records, p => p.CompanyId),
            cancellationToken);

        foreach (var item in items)
        {
            item.ResolvedCompanyName = companyNames.GetValueOrDefault(item.CompanyId);
            item.RemainingDays = (int)(item.ValidityDate.Date - reference).TotalDays;
            item.IsExpired = item.RemainingDays < 0;
        }

        return new PagedResultDto<EmergencyActionPlanListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<EmergencyActionPlanDto> CreateAsync(
        CreateEmergencyActionPlanDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Create);

        var plan = ObjectMapper.Map<CreateEmergencyActionPlanDto, EmergencyActionPlan>(input);

        // The renewal interval of an emergency plan is the same statutory 2/4/6-year rule as the
        // risk assessment, so the single implementation in IRiskAssessmentManager is reused
        // instead of copying the thresholds into this service.
        plan.ValidityDate = riskAssessmentManager.CalculateValidUntilDate(input.PreparedDate, input.HazardClass);

        plan = await planRepository.InsertAsync(plan, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Emergency action plan created: {PlanId} (Company: {CompanyId})", plan.Id, plan.CompanyId);

        return MapPlan(plan);
    }

    /// <inheritdoc />
    public async Task<EmergencyActionPlanDto> UpdateAsync(
        int id,
        UpdateEmergencyActionPlanDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Update);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(EmergencyActionPlan), id);

        ObjectMapper.Map(input, plan);

        plan.ValidityDate = riskAssessmentManager.CalculateValidUntilDate(input.PreparedDate, input.HazardClass);

        plan = await planRepository.UpdateAsync(plan, autoSave: true, cancellationToken);

        return MapPlan(plan);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Delete);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(EmergencyActionPlan), id);

        var sections = await sectionRepository.GetListAsync(s => s.EmergencyActionPlanId == id, cancellationToken);
        var teamMembers = await teamMemberRepository.GetListAsync(m => m.EmergencyActionPlanId == id, cancellationToken);

        await sectionRepository.DeleteManyAsync(sections, autoSave: false, cancellationToken);
        await teamMemberRepository.DeleteManyAsync(teamMembers, autoSave: false, cancellationToken);
        await planRepository.DeleteAsync(plan, autoSave: true, cancellationToken);

        Logger.LogInformation("Emergency action plan deleted: {PlanId}", id);
    }

    // ---------------------------------------------------------------- Sections

    /// <inheritdoc />
    public async Task<ListResultDto<EmergencyPlanSectionDto>> GetSectionsAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Default);

        _ = await planRepository.FindAsync(planId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(EmergencyActionPlan), planId);

        var sections = await sectionRepository.GetListAsync(
            s => s.EmergencyActionPlanId == planId, cancellationToken);

        return new ListResultDto<EmergencyPlanSectionDto>(
        [
            .. ObjectMapper
                .Map<List<EmergencyPlanSection>, List<EmergencyPlanSectionDto>>(sections)
                .OrderBy(s => s.OrderNo)
        ]);
    }

    /// <inheritdoc />
    public async Task<EmergencyPlanSectionDto> SaveSectionAsync(
        int planId,
        EmergencyPlanSectionType sectionType,
        string content,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Update);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new BusinessException(
                    "The emergency plan section content cannot be empty.",
                    "Ensa:EmergencyPlan:SectionContentRequired")
                .WithData("SectionType", sectionType);
        }

        _ = await planRepository.FindAsync(planId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(EmergencyActionPlan), planId);

        // (EmergencyActionPlanId, DepartmentType) is unique, so this is an upsert on one row.
        var section = await sectionRepository.FindAsync(
            s => s.EmergencyActionPlanId == planId && s.SectionType == sectionType,
            cancellationToken);

        if (section is null)
        {
            section = new EmergencyPlanSection
            {
                EmergencyActionPlanId = planId,
                SectionType = sectionType,
                Content = content,
                // Print order follows the section type; the enum values are already in report order.
                OrderNo = (int)sectionType
            };

            section = await sectionRepository.InsertAsync(section, autoSave: true, cancellationToken);
        }
        else
        {
            section.Content = content;
            section = await sectionRepository.UpdateAsync(section, autoSave: true, cancellationToken);
        }

        return ObjectMapper.Map<EmergencyPlanSection, EmergencyPlanSectionDto>(section);
    }

    /// <inheritdoc />
    public async Task RemoveSectionAsync(
        int planId,
        EmergencyPlanSectionType sectionType,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Update);

        var section = await sectionRepository.FindAsync(
                          s => s.EmergencyActionPlanId == planId && s.SectionType == sectionType,
                          cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(EmergencyPlanSection), sectionType);

        await sectionRepository.DeleteAsync(section, autoSave: true, cancellationToken);
    }

    // ------------------------------------------------------------ Team members

    /// <inheritdoc />
    public async Task<ListResultDto<EmergencyTeamMemberDto>> GetTeamMembersAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Default);

        _ = await planRepository.FindAsync(planId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(EmergencyActionPlan), planId);

        var members = await teamMemberRepository.GetListAsync(
            m => m.EmergencyActionPlanId == planId, cancellationToken);

        return new ListResultDto<EmergencyTeamMemberDto>(
            ObjectMapper.Map<List<EmergencyTeamMember>, List<EmergencyTeamMemberDto>>(members));
    }

    /// <inheritdoc />
    public async Task<EmergencyTeamMemberDto> AddTeamMemberAsync(
        int planId,
        CreateEmergencyTeamMemberDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Update);

        _ = await planRepository.FindAsync(planId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(EmergencyActionPlan), planId);

        var member = ObjectMapper.Map<CreateEmergencyTeamMemberDto, EmergencyTeamMember>(input);
        member.EmergencyActionPlanId = planId;

        member = await teamMemberRepository.InsertAsync(member, autoSave: true, cancellationToken);

        return ObjectMapper.Map<EmergencyTeamMember, EmergencyTeamMemberDto>(member);
    }

    /// <inheritdoc />
    public async Task RemoveTeamMemberAsync(
        int planId,
        int teamMemberId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EmergencyPlan.Update);

        var member = await teamMemberRepository.FindAsync(teamMemberId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(EmergencyTeamMember), teamMemberId);

        if (member.EmergencyActionPlanId != planId)
        {
            throw new EntityNotFoundException(typeof(EmergencyTeamMember), teamMemberId);
        }

        await teamMemberRepository.DeleteAsync(member, autoSave: true, cancellationToken);
    }

    // ----------------------------------------------------------------- Helpers

    private EmergencyActionPlanDto MapPlan(EmergencyActionPlan plan)
    {
        var dto = ObjectMapper.Map<EmergencyActionPlan, EmergencyActionPlanDto>(plan);
        dto.IsValid = plan.ValidityDate.Date >= Clock.Now.Date;
        return dto;
    }

    private static Expression<Func<EmergencyActionPlan, bool>>? BuildFilter(
        GetEmergencyActionPlanListInput input,
        DateTime reference)
    {
        var filter = new RiskFilter<EmergencyActionPlan>();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            filter.Add(p =>
                (p.CompanyName != null && p.CompanyName.Contains(search))
                || (p.RegistrationNo != null && p.RegistrationNo.Contains(search))
                || (p.TeamsChief != null && p.TeamsChief.Contains(search)));
        }

        filter.AddIf(input.CompanyId is { }, p => p.CompanyId == input.CompanyId!.Value);
        filter.AddIf(input.HazardClass is { }, p => p.HazardClass == input.HazardClass!.Value);
        if (input.PreparedFrom is { } from)
        {
            filter.Add(p => p.PreparedDate >= from);
        }

        if (input.PreparedTo is { } to)
        {
            filter.Add(p => p.PreparedDate <= to);
        }

        filter.AddIf(input.OnlyExpired, p => p.ValidityDate < reference);

        return filter.Build();
    }
}
