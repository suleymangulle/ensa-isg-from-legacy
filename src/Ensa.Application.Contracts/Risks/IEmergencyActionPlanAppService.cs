using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks;

/// <summary>
/// Emergency action plan application service.
/// <para>
/// Plan text is normalized into <c>EmergencyPlanSection</c> rows keyed by
/// <see cref="EmergencyPlanSectionType"/>, so sections are saved one at a time.
/// </para>
/// </summary>
public interface IEmergencyActionPlanAppService : IApplicationService
{
    Task<EmergencyActionPlanDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Detail projection: plan, company, documents, sections and team members.</summary>
    Task<EmergencyActionPlanNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<EmergencyActionPlanListDto>> GetListAsync(
        GetEmergencyActionPlanListInput input,
        CancellationToken cancellationToken = default);

    Task<EmergencyActionPlanDto> CreateAsync(
        CreateEmergencyActionPlanDto input,
        CancellationToken cancellationToken = default);

    Task<EmergencyActionPlanDto> UpdateAsync(
        int id,
        UpdateEmergencyActionPlanDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ---------------------------------------------------------------- Sections

    Task<ListResultDto<EmergencyPlanSectionDto>> GetSectionsAsync(
        int planId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the single section row for (<paramref name="planId"/>,
    /// <paramref name="sectionType"/>). The print order follows the enum value.
    /// </summary>
    Task<EmergencyPlanSectionDto> SaveSectionAsync(
        int planId,
        EmergencyPlanSectionType sectionType,
        string content,
        CancellationToken cancellationToken = default);

    Task RemoveSectionAsync(
        int planId,
        EmergencyPlanSectionType sectionType,
        CancellationToken cancellationToken = default);

    // ------------------------------------------------------------ Team members

    Task<ListResultDto<EmergencyTeamMemberDto>> GetTeamMembersAsync(
        int planId,
        CancellationToken cancellationToken = default);

    Task<EmergencyTeamMemberDto> AddTeamMemberAsync(
        int planId,
        CreateEmergencyTeamMemberDto input,
        CancellationToken cancellationToken = default);

    Task RemoveTeamMemberAsync(
        int planId,
        int teamMemberId,
        CancellationToken cancellationToken = default);
}
