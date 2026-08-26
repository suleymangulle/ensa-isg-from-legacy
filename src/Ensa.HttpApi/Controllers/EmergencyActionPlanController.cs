using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>Emergency action plan endpoints — <c>api/emergency-action-plan</c>.</summary>
public class EmergencyActionPlanController(IEmergencyActionPlanAppService appService) : EnsaController
{
    /// <summary>Returns a single emergency action plan header.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.EmergencyPlan.Default)]
    [ProducesResponseType<EmergencyActionPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmergencyActionPlanDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Combined detail view: plan, company, documents, sections and team members.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.EmergencyPlan.Default)]
    [ProducesResponseType<EmergencyActionPlanNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmergencyActionPlanNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable emergency action plan list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.EmergencyPlan.Default)]
    [ProducesResponseType<PagedResultDto<EmergencyActionPlanListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<EmergencyActionPlanListDto>> GetListAsync(
        [FromQuery] GetEmergencyActionPlanListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Creates a new emergency action plan.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.EmergencyPlan.Create)]
    [ProducesResponseType<EmergencyActionPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EmergencyActionPlanDto> CreateAsync(
        [FromBody] CreateEmergencyActionPlanDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing emergency action plan.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.EmergencyPlan.Update)]
    [ProducesResponseType<EmergencyActionPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmergencyActionPlanDto> UpdateAsync(
        int id,
        [FromBody] UpdateEmergencyActionPlanDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the plan together with its sections and team members (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.EmergencyPlan.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // ---------------------------------------------------------------- Sections

    /// <summary>Free-text sections of the plan, in print order.</summary>
    [HttpGet("{id:int}/sections")]
    [Authorize(EnsaPermissions.EmergencyPlan.Default)]
    [ProducesResponseType<ListResultDto<EmergencyPlanSectionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<EmergencyPlanSectionDto>> GetSectionsAsync(int id, CancellationToken cancellationToken)
        => appService.GetSectionsAsync(id, cancellationToken);

    /// <summary>Inserts or updates the single section row for the given section type.</summary>
    [HttpPut("{id:int}/sections")]
    [Authorize(EnsaPermissions.EmergencyPlan.Update)]
    [ProducesResponseType<EmergencyPlanSectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EmergencyPlanSectionDto> SaveSectionAsync(
        int id,
        [FromBody] SaveEmergencyPlanSectionDto input,
        CancellationToken cancellationToken)
        => appService.SaveSectionAsync(id, input.SectionType, input.Content, cancellationToken);

    /// <summary>Removes the section of the given type from the plan.</summary>
    [HttpDelete("{id:int}/sections/{sectionType}")]
    [Authorize(EnsaPermissions.EmergencyPlan.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveSectionAsync(
        int id,
        EmergencyPlanSectionType sectionType,
        CancellationToken cancellationToken)
        => appService.RemoveSectionAsync(id, sectionType, cancellationToken);

    // ------------------------------------------------------------ Team members

    /// <summary>Members assigned to the emergency teams of the plan.</summary>
    [HttpGet("{id:int}/team-members")]
    [Authorize(EnsaPermissions.EmergencyPlan.Default)]
    [ProducesResponseType<ListResultDto<EmergencyTeamMemberDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<EmergencyTeamMemberDto>> GetTeamMembersAsync(
        int id,
        CancellationToken cancellationToken)
        => appService.GetTeamMembersAsync(id, cancellationToken);

    /// <summary>Assigns an employee to an emergency team.</summary>
    [HttpPost("{id:int}/team-members")]
    [Authorize(EnsaPermissions.EmergencyPlan.Update)]
    [ProducesResponseType<EmergencyTeamMemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EmergencyTeamMemberDto> AddTeamMemberAsync(
        int id,
        [FromBody] CreateEmergencyTeamMemberDto input,
        CancellationToken cancellationToken)
        => appService.AddTeamMemberAsync(id, input, cancellationToken);

    /// <summary>Removes a member from the emergency teams of the plan.</summary>
    [HttpDelete("{id:int}/team-members/{teamMemberId:int}")]
    [Authorize(EnsaPermissions.EmergencyPlan.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveTeamMemberAsync(int id, int teamMemberId, CancellationToken cancellationToken)
        => appService.RemoveTeamMemberAsync(id, teamMemberId, cancellationToken);
}
