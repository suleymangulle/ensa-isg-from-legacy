using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health;
using Ensa.Application.Contracts.Health.Dtos;
using Ensa.Application.Contracts.Health.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Health surveillance (EK-2) medical examination form endpoints — <c>api/medical-examination-form</c>.
/// <para>
/// <b>PRIVACY.</b> These endpoints serve special-category health data. The collection
/// endpoints return a clinical-free projection; clinical content is only reachable one
/// record at a time through <c>{id}</c> and <c>{id}/detail</c>.
/// </para>
/// </summary>
public class MedicalExaminationFormController(IMedicalExaminationFormAppService appService) : EnsaController
{
    /// <summary>Returns one form including its clinical fields.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.MedicalExamination.Default)]
    [ProducesResponseType<MedicalExaminationFormDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MedicalExaminationFormDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Form with the employee, the workplace and all six child collections.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.MedicalExamination.Default)]
    [ProducesResponseType<MedicalExaminationFormNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MedicalExaminationFormNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable list. Carries no clinical fields.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.MedicalExamination.Default)]
    [ProducesResponseType<PagedResultDto<MedicalExaminationFormListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<MedicalExaminationFormListDto>> GetListAsync(
        [FromQuery] GetMedicalExaminationFormListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Most recent examination of an employee.</summary>
    [HttpGet("employee/{employeeId:int}/latest")]
    [Authorize(EnsaPermissions.MedicalExamination.Default)]
    [ProducesResponseType<MedicalExaminationFormDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<MedicalExaminationFormDto?> GetLatestForEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken)
        => appService.GetLatestForEmployeeAsync(employeeId, cancellationToken);

    /// <summary>Forms of a workplace whose validity has lapsed as of the given date.</summary>
    [HttpGet("company/{companyId:int}/expiring")]
    [Authorize(EnsaPermissions.MedicalExamination.Default)]
    [ProducesResponseType<ListResultDto<MedicalExaminationFormListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<MedicalExaminationFormListDto>> GetExpiringAsync(
        int companyId,
        [FromQuery] DateTime asOf,
        CancellationToken cancellationToken)
        => appService.GetExpiringAsync(companyId, asOf, cancellationToken);

    /// <summary>Creates a new examination form.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.MedicalExamination.Create)]
    [ProducesResponseType<MedicalExaminationFormDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<MedicalExaminationFormDto> CreateAsync(
        [FromBody] CreateMedicalExaminationFormDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an examination form.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.MedicalExamination.Update)]
    [ProducesResponseType<MedicalExaminationFormDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MedicalExaminationFormDto> UpdateAsync(
        int id,
        [FromBody] UpdateMedicalExaminationFormDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes an examination form together with its clinical child rows.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.MedicalExamination.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // ------------------------------------------------------------------
    // Child collections — every PUT replaces the whole set for one form.
    // ------------------------------------------------------------------

    /// <summary>Replaces the complaint set of a form.</summary>
    [HttpPut("{id:int}/complaints")]
    [Authorize(EnsaPermissions.MedicalExamination.Update)]
    [ProducesResponseType<ListResultDto<MedicalExamComplaintDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<MedicalExamComplaintDto>> SaveComplaintsAsync(
        int id,
        [FromBody] List<SaveMedicalExamComplaintDto> input,
        CancellationToken cancellationToken)
        => appService.SaveComplaintsAsync(id, input, cancellationToken);

    /// <summary>Replaces the physical finding set of a form.</summary>
    [HttpPut("{id:int}/physical-findings")]
    [Authorize(EnsaPermissions.MedicalExamination.Update)]
    [ProducesResponseType<ListResultDto<MedicalExamPhysicalFindingDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<MedicalExamPhysicalFindingDto>> SavePhysicalFindingsAsync(
        int id,
        [FromBody] List<SaveMedicalExamPhysicalFindingDto> input,
        CancellationToken cancellationToken)
        => appService.SavePhysicalFindingsAsync(id, input, cancellationToken);

    /// <summary>Replaces the laboratory test set of a form.</summary>
    [HttpPut("{id:int}/lab-tests")]
    [Authorize(EnsaPermissions.MedicalExamination.Update)]
    [ProducesResponseType<ListResultDto<MedicalExamLabTestDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<MedicalExamLabTestDto>> SaveLabTestsAsync(
        int id,
        [FromBody] List<SaveMedicalExamLabTestDto> input,
        CancellationToken cancellationToken)
        => appService.SaveLabTestsAsync(id, input, cancellationToken);

    /// <summary>Replaces the habit set of a form.</summary>
    [HttpPut("{id:int}/habits")]
    [Authorize(EnsaPermissions.MedicalExamination.Update)]
    [ProducesResponseType<ListResultDto<MedicalExamHabitDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<MedicalExamHabitDto>> SaveHabitsAsync(
        int id,
        [FromBody] List<SaveMedicalExamHabitDto> input,
        CancellationToken cancellationToken)
        => appService.SaveHabitsAsync(id, input, cancellationToken);

    /// <summary>Replaces the working condition assessment set of a form.</summary>
    [HttpPut("{id:int}/work-conditions")]
    [Authorize(EnsaPermissions.MedicalExamination.Update)]
    [ProducesResponseType<ListResultDto<MedicalExamWorkConditionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<MedicalExamWorkConditionDto>> SaveWorkConditionsAsync(
        int id,
        [FromBody] List<SaveMedicalExamWorkConditionDto> input,
        CancellationToken cancellationToken)
        => appService.SaveWorkConditionsAsync(id, input, cancellationToken);

    /// <summary>Replaces the immunisation declaration set of a form.</summary>
    [HttpPut("{id:int}/immunizations")]
    [Authorize(EnsaPermissions.MedicalExamination.Update)]
    [ProducesResponseType<ListResultDto<MedicalExamImmunizationDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<MedicalExamImmunizationDto>> SaveImmunizationsAsync(
        int id,
        [FromBody] List<SaveMedicalExamImmunizationDto> input,
        CancellationToken cancellationToken)
        => appService.SaveImmunizationsAsync(id, input, cancellationToken);
}
