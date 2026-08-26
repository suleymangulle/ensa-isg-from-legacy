using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health.Dtos;
using Ensa.Application.Contracts.Health.Dtos.Navigations;

namespace Ensa.Application.Contracts.Health;

/// <summary>
/// Health surveillance (EK-2) medical examination form application service.
/// <para>
/// <b>PRIVACY.</b> Everything this service returns except <see cref="GetListAsync"/> and
/// <see cref="GetExpiringAsync"/> contains special-category health data. Those two
/// collection-returning methods deliberately project to
/// <see cref="MedicalExaminationFormListDto"/>, which carries no clinical fields.
/// </para>
/// <para>
/// The statutory examination interval (low 5 years, hazardous 3 years, very hazardous
/// 1 year), the next-examination date and the body mass index are owned by
/// <c>IHealthSurveillanceManager</c>; this service calls it and never re-implements the rules.
/// </para>
/// </summary>
public interface IMedicalExaminationFormAppService : IApplicationService
{
    /// <summary>Returns one form including its clinical fields.</summary>
    Task<MedicalExaminationFormDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the form together with the employee, the workplace and all six
    /// normalised child collections in a single call.
    /// </summary>
    Task<MedicalExaminationFormNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Paged, filterable list. Carries no clinical fields — see the DTO remarks.</summary>
    Task<PagedResultDto<MedicalExaminationFormListDto>> GetListAsync(
        GetMedicalExaminationFormListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Most recent examination of an employee, or <c>null</c> when there is none.</summary>
    Task<MedicalExaminationFormDto?> GetLatestForEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forms of a workplace whose validity has lapsed as of <paramref name="asOf"/> —
    /// the periodic follow-up warning list. Carries no clinical fields.
    /// </summary>
    Task<ListResultDto<MedicalExaminationFormListDto>> GetExpiringAsync(
        int companyId,
        DateTime asOf,
        CancellationToken cancellationToken = default);

    Task<MedicalExaminationFormDto> CreateAsync(
        CreateMedicalExaminationFormDto input,
        CancellationToken cancellationToken = default);

    Task<MedicalExaminationFormDto> UpdateAsync(
        int id,
        UpdateMedicalExaminationFormDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------------
    // Child collections — each call REPLACES the whole set for one form.
    // ------------------------------------------------------------------

    /// <summary>Replaces the complaint set of a form.</summary>
    Task<ListResultDto<MedicalExamComplaintDto>> SaveComplaintsAsync(
        int formId,
        List<SaveMedicalExamComplaintDto> input,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the physical finding set of a form.</summary>
    Task<ListResultDto<MedicalExamPhysicalFindingDto>> SavePhysicalFindingsAsync(
        int formId,
        List<SaveMedicalExamPhysicalFindingDto> input,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the laboratory test set of a form.</summary>
    Task<ListResultDto<MedicalExamLabTestDto>> SaveLabTestsAsync(
        int formId,
        List<SaveMedicalExamLabTestDto> input,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the habit set of a form.</summary>
    Task<ListResultDto<MedicalExamHabitDto>> SaveHabitsAsync(
        int formId,
        List<SaveMedicalExamHabitDto> input,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the working condition assessment set of a form.</summary>
    Task<ListResultDto<MedicalExamWorkConditionDto>> SaveWorkConditionsAsync(
        int formId,
        List<SaveMedicalExamWorkConditionDto> input,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the immunisation declaration set of a form.</summary>
    Task<ListResultDto<MedicalExamImmunizationDto>> SaveImmunizationsAsync(
        int formId,
        List<SaveMedicalExamImmunizationDto> input,
        CancellationToken cancellationToken = default);
}
